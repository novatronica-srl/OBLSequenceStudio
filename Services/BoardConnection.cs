using System.Diagnostics;
using System.IO.Ports;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Services;

public enum BoardCommand : ushort { Load=1, Apply=2, Start=3, Pause=4, Resume=5, Stop=6, Restart=7, Save=8 }
public sealed class BoardCommandException(ushort result, ushort step, ushort field)
    : Exception(Localizer.Text("BoardCommandError", result, step, field))
{
    public ushort Result { get; } = result;
}
public sealed record BoardTelemetry(int[] Pwm, ushort[] States, ushort[] Steps, uint[] Elapsed,
    uint Revision, ushort SaveState, bool Dirty);

public sealed class BoardConnection : IDisposable
{
    private readonly SerialPort _port;
    private readonly IRegisterClient _master;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;
    private ushort _commandSequence;
    private readonly Dictionary<int, ChannelSequence> _applied;
    public string Description { get; }
    public DeviceSequences Configuration { get; }
    public string PortName => _port.PortName;

    internal BoardConnection(SerialPort port, IRegisterClient master, ushort[] metadata)
    {
        ValidateMetadata(metadata);
        _port = port; _master = master; _commandSequence = metadata[8];
        string version = $"{metadata[1] >> 8}.{metadata[1] & 255}";
        Description = $"{port.PortName} · {port.BaudRate} · {port.Parity} · FW {version}";
        Configuration = new DeviceSequences { SourceBoardPID = "1", SourceBoardVersion = version };
        uint revision = ((uint)metadata[12] << 16) | metadata[13];
        for (ushort channel = 1; channel <= 28; channel++)
        {
            Execute(BoardCommand.Load, channel);
            var words = _master.ReadHoldingRegisters(1, 8, 100).Concat(_master.ReadHoldingRegisters(1, 108, 42)).ToArray();
            Configuration.Channels.Add(Decode(channel, words));
        }
        var after = _master.ReadInputRegisters(1, 12, 2);
        if ((((uint)after[0] << 16) | after[1]) != revision) throw new IOException(Localizer.Text("BoardChangedDuringRead"));
        SequenceService.Validate(Configuration);
        _applied = Configuration.Channels.ToDictionary(c => c.ChannelNumber, c => c.Clone(c.ChannelNumber));
    }

    internal static void ValidateMetadata(ushort[] m)
    {
        if (m.Length < 20 || m[0] != 1 || m[1] >> 8 != 2 || m[2] != 0x0200 || (m[3] & 15) != 15 ||
            m[4] != 28 || m[5] != 10 || m[6] != 14 || m[7] != 142)
            throw new InvalidDataException(Localizer.Text("UnsupportedProtocol"));
    }

    public static Task<BoardConnection?> FindAsync(IProgress<string> progress, CancellationToken cancellation) => Task.Run(() =>
    {
        ConnectionDiagnostics.Begin();
        bool oldFirmware = false;
        foreach (string name in SerialPort.GetPortNames().OrderBy(n => n))
        foreach (int baud in new[] { 38400, 19200, 9600 })
        foreach (Parity parity in new[] { Parity.None, Parity.Even, Parity.Odd })
        {
            cancellation.ThrowIfCancellationRequested();
            progress.Report($"{name} · {baud} · {parity}");
            ConnectionDiagnostics.Write($"OPEN {name} {baud} 8{parity}1 slave=1");
            SerialPort? port = null; bool identified = false;
            try
            {
                port = new SerialPort(name, baud, parity, 8, StopBits.One) { ReadTimeout = 500, WriteTimeout = 1000, Handshake = Handshake.None };
                port.Open();
                if (cancellation.WaitHandle.WaitOne(100)) cancellation.ThrowIfCancellationRequested();
                var master = new ModbusRtuMaster(port);
                var identity = ReadIdentity(master, cancellation);
                ConnectionDiagnostics.Write("IDENTITY " + string.Join(", ", identity.Select(v => $"0x{v:X4}")));
                if (identity[0] != 1) continue;
                if (identity[1] >> 8 != 2) { oldFirmware = true; continue; }
                identified = true;
                port.ReadTimeout = 1500;
                port.BaseStream.ReadTimeout = 1500;
                var metadata = master.ReadInputRegisters(1, 0, 20);
                cancellation.ThrowIfCancellationRequested();
                // Registering disposal also bounds cancellation during the 28-channel load.
                var ownedPort = port;
                using var registration = cancellation.Register(() => ownedPort.Dispose());
                var connection = new BoardConnection(port, master, metadata);
                ConnectionDiagnostics.Write("CONNECTED " + connection.Description);
                cancellation.ThrowIfCancellationRequested();
                port = null;
                return connection;
            }
            catch (Exception) when (cancellation.IsCancellationRequested) { throw new OperationCanceledException(cancellation); }
            catch (Exception ex) when (!identified && ex is IOException or TimeoutException or UnauthorizedAccessException or InvalidOperationException)
            { ConnectionDiagnostics.Write($"PROBE FAILED {ex.GetType().Name}: {ex.Message}"); }
            catch (Exception ex) { ConnectionDiagnostics.Write($"LOAD FAILED {ex.GetType().Name}: {ex.Message}"); throw; }
            finally { port?.Dispose(); }
        }
        if (oldFirmware) throw new InvalidDataException(Localizer.Text("UnsupportedProtocol"));
        return null;
    }, cancellation);

    internal static ushort[] ReadIdentity(IRegisterClient master, CancellationToken cancellation)
    {
        for (int attempt = 1; ; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            try { return master.ReadInputRegisters(1, 0, 2); }
            catch (Exception ex) when (attempt < 3 && ex is IOException or TimeoutException)
            {
                ConnectionDiagnostics.Write($"IDENTITY attempt {attempt}: {ex.GetType().Name}: {ex.Message}");
                if (cancellation.WaitHandle.WaitOne(100)) cancellation.ThrowIfCancellationRequested();
            }
        }
    }

    public static ushort[] Encode(ChannelSequence channel)
    {
        var validation = DeviceSequences.CreateDefault();
        validation.Channels[channel.ChannelNumber - 1] = channel;
        SequenceService.Validate(validation);
        ushort[] words = new ushort[142];
        words[0] = (ushort)channel.Steps.Count; words[1] = 1; // Editor sequences always cycle.
        for (int i = 0; i < channel.Steps.Count; i++)
        {
            var s = channel.Steps[i];
            int[] fields = { (int)s.Mode, s.Duration, s.Start, s.End, s.Brightness, s.Minimum, s.Maximum,
                s.Cycle, s.Duty, s.Mean, s.StdDev, s.Filter, s.Refresh, 0 };
            for (int f = 0; f < 14; f++) words[2 + i * 14 + f] = checked((ushort)fields[f]);
        }
        return words;
    }

    internal static ChannelSequence Decode(int channel, ushort[] words)
    {
        if (words.Length != 142 || words[0] > 10 || words[1] > 1) throw new InvalidDataException(Localizer.Text("InvalidProgrammerConfig"));
        var result = new ChannelSequence { ChannelNumber = channel, Name = Localizer.Text("ChannelName", channel), Repeat = words[1] != 0 };
        for (int i = 0; i < words[0]; i++)
        {
            int n = 2 + i * 14;
            if (words[n + 13] != 0) throw new InvalidDataException(Localizer.Text("UnsupportedProtocol"));
            result.Steps.Add(new SequenceStep { Mode = (StepMode)words[n], Duration = words[n + 1], Start = words[n + 2], End = words[n + 3],
                Brightness = words[n + 4], Minimum = words[n + 5], Maximum = words[n + 6], Cycle = words[n + 7], Duty = words[n + 8],
                Mean = words[n + 9], StdDev = words[n + 10], Filter = words[n + 11], Refresh = words[n + 12] });
        }
        return result;
    }

    private void Execute(BoardCommand command, ushort channel, ushort options = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _master.WriteMultipleRegisters(1, 1, new[] { (ushort)command, channel, options });
        ushort number = unchecked(++_commandSequence);
        _master.WriteSingleRegister(1, 0, number); // Trigger LAST, separate transaction.
        var timeout = Stopwatch.StartNew();
        while (timeout.ElapsedMilliseconds < 5000)
        {
            var ack = _master.ReadInputRegisters(1, 8, 4);
            if (ack[0] == number)
            {
                if (ack[1] != 0) throw new BoardCommandException(ack[1], ack[2], ack[3]);
                return;
            }
            Thread.Sleep(10);
        }
        throw new TimeoutException(Localizer.Text("BoardAckTimeout"));
    }

    private async Task<T> RunAsync<T>(Func<T> action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { return await Task.Run(() => { ObjectDisposedException.ThrowIf(_disposed, this); return action(); }).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public Task<int> ApplyAsync(DeviceSequences snapshot, bool start = true, int target = 0) => RunAsync(() =>
    {
        SequenceService.Validate(snapshot);
        SequenceService.EnsureCyclic(snapshot);
        if (target is < 0 or > 28) throw new ArgumentOutOfRangeException(nameof(target));
        var changed = snapshot.Channels.Where(c => target == 0 || c.ChannelNumber == target).Where(c => !_applied.TryGetValue(c.ChannelNumber, out var p) ||
            c.Repeat != p.Repeat || c.Steps.Count != p.Steps.Count || !c.Steps.Zip(p.Steps).All(pair => pair.First.SameValues(pair.Second)))
            .Select(c => (Channel: c, Words: Encode(c))).ToArray();
        foreach (var (channel, words) in changed)
        {
            _master.WriteMultipleRegisters(1, 8, words[..100]);
            _master.WriteMultipleRegisters(1, 108, words[100..]);
            Execute(BoardCommand.Apply, (ushort)channel.ChannelNumber, start ? (ushort)1 : (ushort)0);
            _applied[channel.ChannelNumber] = channel.Clone(channel.ChannelNumber);
        }
        return changed.Length;
    });

    public Task CommandAsync(BoardCommand command, int channel = 0) => RunAsync(() =>
    {
        if (command is BoardCommand.Load or BoardCommand.Apply or BoardCommand.Save || channel is < 0 or > 28) throw new ArgumentOutOfRangeException(nameof(command));
        Execute(command, (ushort)channel); return 0;
    });

    public Task SaveAsync() => RunAsync(() =>
    {
        Execute(BoardCommand.Save, 0);
        var timeout = Stopwatch.StartNew();
        while (timeout.ElapsedMilliseconds < 20000)
        {
            var state = _master.ReadInputRegisters(1, 14, 1)[0];
            if (state == 2) return 0;
            if (state == 3) throw new BoardCommandException(5, 0, 0xffff);
            Thread.Sleep(25);
        }
        throw new TimeoutException(Localizer.Text("BoardSaveTimeout"));
    });

    public Task<BoardTelemetry> ReadTelemetryAsync() => RunAsync(() =>
    {
        // This board's FreeModbus handler rejects count=125 (strict <125 check).
        // Keep reads below that boundary, including on already deployed firmware.
        var r = _master.ReadInputRegisters(1, 0, 100).Concat(_master.ReadInputRegisters(1, 100, 60)).ToArray();
        ValidateMetadata(r);
        return new BoardTelemetry(r[20..48].Select(v => Math.Clamp((int)v, 0, 100)).ToArray(), r[48..76], r[76..104],
            Enumerable.Range(0, 28).Select(i => ((uint)r[104 + i] << 16) | r[132 + i]).ToArray(),
            ((uint)r[12] << 16) | r[13], r[14], r[15] != 0);
    });
    public void Dispose() { _disposed = true; _port.Dispose(); _master.Dispose(); }
}
