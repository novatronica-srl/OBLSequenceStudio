using System.Buffers.Binary;
using System.Security.Cryptography;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.STBootLib;

namespace OBLSequenceStudio.Services;

/// <summary>Validated STM32 application image with an immutable, word-aligned programming buffer.</summary>
public sealed class FirmwareImage
{
    /// <summary>Application flash base address used for programming and reset-vector validation.</summary>
    public const uint FlashAddress = 0x08000000;
    /// <summary>Required target flash capacity in bytes: 128 KiB.</summary>
    public const int FlashBytes = 128 * 1024;
    private readonly byte[] _data;
    /// <summary>Original binary length in bytes, before programming alignment padding.</summary>
    public int FileLength { get; }
    /// <summary>Uppercase SHA-256 fingerprint of the original binary, excluding alignment padding.</summary>
    public string Sha256 { get; }
    /// <summary>Returns a defensive copy padded to four-byte alignment with erased-flash bytes (0xFF).</summary>
    internal byte[] CopyData() => _data.ToArray();
    private FirmwareImage(byte[] original)
    {
        FileLength = original.Length;
        Sha256 = Convert.ToHexString(SHA256.HashData(original));
        _data = Enumerable.Repeat((byte)0xff, (original.Length + 3) & ~3).ToArray();
        original.CopyTo(_data, 0);
    }
    /// <summary>Loads a .bin file and validates its size and initial stack and reset vectors.</summary>
    /// <exception cref="InvalidDataException">The file extension, size or vector table is unsuitable for the supported target.</exception>
    public static FirmwareImage Load(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".bin", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        var info = new FileInfo(path);
        if (info.Length is < 8 or > FlashBytes) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        return FromBytes(File.ReadAllBytes(path));
    }
    /// <summary>Validates the SRAM stack range, eight-byte stack alignment and Thumb reset address before copying image data.</summary>
    internal static FirmwareImage FromBytes(byte[] bytes)
    {
        if (bytes.Length is < 8 or > FlashBytes) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        uint stack = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4));
        uint reset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4));
        if (stack <= 0x20000000 || stack > 0x20005000 || (stack & 7) != 0 || (reset & 1) == 0 ||
            (reset & ~1u) < FlashAddress + 8 || (reset & ~1u) >= FlashAddress + bytes.Length)
            throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        return new FirmwareImage(bytes.ToArray());
    }
}

/// <summary>Isolates bootloader discovery, erase and memory transfers from the concrete serial implementation.</summary>
internal interface IBootloader : IDisposable
{
    ushort ProductId { get; }
    Task InitializeAsync();
    Task EraseAsync();
    /// <summary>Reads the requested buffer from the target address and reports transferred bytes.</summary>
    Task ReadAsync(uint address, byte[] bytes, IProgress<int>? progress, CancellationToken token);
    /// <summary>Writes the buffer to the target address and reports processed and total bytes.</summary>
    Task WriteAsync(uint address, byte[] bytes, IProgress<STBootProgress>? progress, CancellationToken token);
}

/// <summary>Adapts STBootLib to the firmware workflow and owns the bootloader serial connection.</summary>
internal sealed class STBootAdapter : IBootloader
{
    private readonly STBoot _boot = new();
    /// <summary>Opens the selected bootloader port at 115200 baud and releases resources if opening fails.</summary>
    public STBootAdapter(string port) { try { _boot.Open(port, 115200); } catch { _boot.Dispose(); throw; } }
    /// <summary>Product identifier discovered during the bootloader handshake.</summary>
    public ushort ProductId => _boot.ProductID;
    /// <summary>Performs the bootloader handshake and reads the supported command list and product identifier.</summary>
    public Task InitializeAsync() => _boot.Initialize();
    /// <summary>Erases target flash using the erase command supported by the detected bootloader.</summary>
    public Task EraseAsync() => _boot.GlobalErase();
    /// <summary>Delegates a cancellable read of the entire buffer to STBootLib.</summary>
    public Task ReadAsync(uint address, byte[] bytes, IProgress<int>? progress, CancellationToken token) => _boot.ReadMemory(address, bytes, 0, bytes.Length, progress, token);
    /// <summary>Delegates a cancellable write of the entire buffer to STBootLib.</summary>
    public Task WriteAsync(uint address, byte[] bytes, IProgress<STBootProgress>? progress, CancellationToken token) => _boot.WriteMemory(address, bytes, 0, bytes.Length, progress, token);
    /// <summary>Releases the bootloader transport owned by this adapter.</summary>
    public void Dispose() => _boot.Dispose();
}

/// <summary>Programming stage resource key and overall progress percentage.</summary>
/// <param name="StageKey">Localization key describing the current operation.</param>
/// <param name="Percent">Overall progress from 0 to 100.</param>
public sealed record FirmwareProgress(string StageKey, int Percent);
/// <summary>Owns a validated bootloader connection for a single erase, program and readback verification attempt.</summary>
public sealed class FirmwareUpdateSession : IDisposable
{
    private readonly IBootloader _boot;
    private bool _ready, _used;
    /// <summary>Detected STM32 product identifier used to enforce target compatibility.</summary>
    public ushort ProductId => _boot.ProductId;
    /// <summary>Detected flash capacity in bytes, populated by probing the target.</summary>
    public int FlashSize { get; private set; }
    /// <summary>Takes ownership of a bootloader transport; probing must succeed before programming.</summary>
    internal FirmwareUpdateSession(IBootloader boot) => _boot = boot;

    /// <summary>Opens and probes the selected port, disposing the session if target validation fails.</summary>
    /// <param name="port">Serial port name of a board already started in its system bootloader.</param>
    /// <returns>A ready session whose ownership passes to the caller.</returns>
    public static async Task<FirmwareUpdateSession> OpenAsync(string port)
    {
        var session = new FirmwareUpdateSession(await Task.Run(() => new STBootAdapter(port)).ConfigureAwait(false));
        try { await session.ProbeAsync().ConfigureAwait(false); return session; }
        catch { session.Dispose(); throw; }
    }
    /// <summary>Requires product 0x0410 and 128 KiB flash before allowing any erase operation.</summary>
    internal async Task ProbeAsync()
    {
        await _boot.InitializeAsync().ConfigureAwait(false);
        if (ProductId != 0x0410) throw new InvalidDataException(Localizer.Text("FirmwareTargetInvalid", ProductId));
        byte[] capacity = new byte[2];
        await _boot.ReadAsync(0x1ffff7e0, capacity, null, CancellationToken.None).ConfigureAwait(false);
        FlashSize = BinaryPrimitives.ReadUInt16LittleEndian(capacity) * 1024;
        if (FlashSize != FirmwareImage.FlashBytes) throw new InvalidDataException(Localizer.Text("FirmwareTargetInvalid", ProductId));
        _ready = true;
    }
    /// <summary>Erases flash, writes an aligned image and compares the complete readback with the programming buffer.</summary>
    /// <param name="image">Previously validated application image.</param>
    /// <param name="progress">Receives localized stage keys and overall progress percentages.</param>
    /// <param name="token">Cancellation checked between stages and during chunked transfers; erase itself is not cancellable.</param>
    /// <exception cref="InvalidOperationException">The session was not probed or a programming attempt has already started.</exception>
    /// <exception cref="InvalidDataException">The image exceeds target capacity or readback differs.</exception>
    public async Task ProgramAsync(FirmwareImage image, IProgress<FirmwareProgress> progress, CancellationToken token = default)
    {
        if (!_ready || _used) throw new InvalidOperationException(Localizer.Text("FirmwareReconnectBoot"));
        byte[] data = image.CopyData();
        if (data.Length > FlashSize) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        token.ThrowIfCancellationRequested();
        // An interrupted attempt may leave flash partially erased or written; require a new session to retry.
        _used = true;
        progress.Report(new("FirmwareErasing", 0));
        await _boot.EraseAsync().ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        progress.Report(new("FirmwareWriting", 10));
        await _boot.WriteAsync(FirmwareImage.FlashAddress, data,
            new InlineProgress<STBootProgress>(p => progress.Report(new("FirmwareWriting", 10 + p.bytesProcessed * 65 / data.Length))), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        progress.Report(new("FirmwareVerifying", 75));
        byte[] actual = new byte[data.Length];
        await _boot.ReadAsync(FirmwareImage.FlashAddress, actual,
            new InlineProgress<int>(n => progress.Report(new("FirmwareVerifying", 75 + n * 24 / data.Length))), token).ConfigureAwait(false);
        if (!actual.AsSpan().SequenceEqual(data)) throw new InvalidDataException(Localizer.Text("FirmwareVerifyFailed"));
        progress.Report(new("FirmwareVerified", 100));
    }
    /// <summary>Releases the bootloader transport owned by this session.</summary>
    public void Dispose() => _boot.Dispose();
    private sealed class InlineProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
}
