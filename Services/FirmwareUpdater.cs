using System.Buffers.Binary;
using System.Security.Cryptography;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.STBootLib;

namespace OBLSequenceStudio.Services;

public sealed class FirmwareImage
{
    public const uint FlashAddress = 0x08000000;
    public const int FlashBytes = 128 * 1024;
    private readonly byte[] _data;
    public int FileLength { get; }
    public string Sha256 { get; }
    internal byte[] CopyData() => _data.ToArray();
    private FirmwareImage(byte[] original)
    {
        FileLength = original.Length;
        Sha256 = Convert.ToHexString(SHA256.HashData(original));
        _data = Enumerable.Repeat((byte)0xff, (original.Length + 3) & ~3).ToArray();
        original.CopyTo(_data, 0);
    }
    public static FirmwareImage Load(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".bin", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        var info = new FileInfo(path);
        if (info.Length is < 8 or > FlashBytes) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        return FromBytes(File.ReadAllBytes(path));
    }
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

internal interface IBootloader : IDisposable
{
    ushort ProductId { get; }
    Task InitializeAsync();
    Task EraseAsync();
    Task ReadAsync(uint address, byte[] bytes, IProgress<int>? progress, CancellationToken token);
    Task WriteAsync(uint address, byte[] bytes, IProgress<STBootProgress>? progress, CancellationToken token);
}

internal sealed class STBootAdapter : IBootloader
{
    private readonly STBoot _boot = new();
    public STBootAdapter(string port) { try { _boot.Open(port, 115200); } catch { _boot.Dispose(); throw; } }
    public ushort ProductId => _boot.ProductID;
    public Task InitializeAsync() => _boot.Initialize();
    public Task EraseAsync() => _boot.GlobalErase();
    public Task ReadAsync(uint address, byte[] bytes, IProgress<int>? progress, CancellationToken token) => _boot.ReadMemory(address, bytes, 0, bytes.Length, progress, token);
    public Task WriteAsync(uint address, byte[] bytes, IProgress<STBootProgress>? progress, CancellationToken token) => _boot.WriteMemory(address, bytes, 0, bytes.Length, progress, token);
    public void Dispose() => _boot.Dispose();
}

public sealed record FirmwareProgress(string StageKey, int Percent);
public sealed class FirmwareUpdateSession : IDisposable
{
    private readonly IBootloader _boot;
    private bool _ready, _used;
    public ushort ProductId => _boot.ProductId;
    public int FlashSize { get; private set; }
    internal FirmwareUpdateSession(IBootloader boot) => _boot = boot;

    public static async Task<FirmwareUpdateSession> OpenAsync(string port)
    {
        var session = new FirmwareUpdateSession(await Task.Run(() => new STBootAdapter(port)).ConfigureAwait(false));
        try { await session.ProbeAsync().ConfigureAwait(false); return session; }
        catch { session.Dispose(); throw; }
    }
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
    public async Task ProgramAsync(FirmwareImage image, IProgress<FirmwareProgress> progress, CancellationToken token = default)
    {
        if (!_ready || _used) throw new InvalidOperationException(Localizer.Text("FirmwareReconnectBoot"));
        byte[] data = image.CopyData();
        if (data.Length > FlashSize) throw new InvalidDataException(Localizer.Text("FirmwareImageInvalid"));
        token.ThrowIfCancellationRequested();
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
    public void Dispose() => _boot.Dispose();
    private sealed class InlineProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
}
