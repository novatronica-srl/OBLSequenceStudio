namespace OBLSequenceStudio.Services;

/// <summary>Modbus RTU functions 03, 04, 06 and 16, with response and CRC validation.</summary>
public sealed class ModbusRtuMaster : IRegisterClient
{
    private readonly Stream transport;
    private readonly Action discardInput;
    private readonly int baudRate;
    private readonly Action<string>? trace;
    private readonly object sync = new();
    /// <summary>Uses an already opened serial port; ownership remains with the caller.</summary>
    public ModbusRtuMaster(System.IO.Ports.SerialPort port)
        : this(port.BaseStream, port.BaudRate, port.DiscardInBuffer, ConnectionDiagnostics.Write)
    { transport.ReadTimeout = port.ReadTimeout; transport.WriteTimeout = port.WriteTimeout; }
    /// <summary>Creates a register client over a caller-owned stream with configurable input flushing and frame tracing.</summary>
    internal ModbusRtuMaster(Stream transport, int baudRate = 38400, Action? discardInput = null, Action<string>? trace = null)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if (baudRate <= 0) throw new ArgumentOutOfRangeException(nameof(baudRate));
        this.baudRate = baudRate;
        this.discardInput = discardInput ?? (() => { });
        this.trace = trace;
    }
    /// <summary>Reads 1 to 125 holding registers with function 03 and validates the returned byte count.</summary>
    public ushort[] ReadHoldingRegisters(byte slave, ushort address, ushort count) => Read(slave, 3, address, count);
    /// <summary>Reads 1 to 125 input registers with function 04 and validates the returned byte count.</summary>
    public ushort[] ReadInputRegisters(byte slave, ushort address, ushort count) => Read(slave, 4, address, count);
    private ushort[] Read(byte slave, byte function, ushort address, ushort count)
    {
        if (count is < 1 or > 125) throw new ArgumentOutOfRangeException(nameof(count));
        var response = Exchange(new byte[] { slave, function, (byte)(address >> 8), (byte)address, (byte)(count >> 8), (byte)count });
        if (response[2] != count * 2) throw new IOException("Invalid Modbus register count.");
        var values = new ushort[count];
        for (int i = 0; i < count; i++) values[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
        return values;
    }
    /// <summary>Writes one holding register using function 06 and validates the echoed address and value.</summary>
    public void WriteSingleRegister(byte slave, ushort address, ushort value) => Exchange(new byte[] { slave, 6, (byte)(address >> 8), (byte)address, (byte)(value >> 8), (byte)value });
    /// <summary>Writes 1 to 123 holding registers using function 16 with big-endian register bytes.</summary>
    public void WriteMultipleRegisters(byte slave, ushort address, ushort[] values)
    {
        if (values.Length is < 1 or > 123) throw new ArgumentOutOfRangeException(nameof(values));
        var request = new byte[7 + values.Length * 2];
        request[0] = slave; request[1] = 16; request[2] = (byte)(address >> 8); request[3] = (byte)address;
        request[4] = (byte)(values.Length >> 8); request[5] = (byte)values.Length; request[6] = (byte)(values.Length * 2);
        for (int i = 0; i < values.Length; i++) { request[7 + i * 2] = (byte)(values[i] >> 8); request[8 + i * 2] = (byte)values[i]; }
        Exchange(request);
    }
    /// <summary>Locks the transport for a complete RTU request and validates response CRC, slave, function and write acknowledgement.</summary>
    private byte[] Exchange(byte[] request)
    {
        lock (sync)
        {
            // RTU requires a silent interval between frames (at least 3.5 characters).
            Thread.Sleep(Math.Max(2, (int)Math.Ceiling(38500.0 / baudRate)));
            // discardInput may reference SerialPort.DiscardInBuffer which throws InvalidOperationException
            // when the port is closed. Swallow that specific exception here � higher-level
            // read/write operations will surface appropriate errors if the port is closed.
            try { discardInput(); }
            catch (InvalidOperationException) { }
            var frame = new byte[request.Length + 2];
            request.CopyTo(frame, 0);
            ushort crc = Crc(request);
            frame[^2] = (byte)crc; frame[^1] = (byte)(crc >> 8);
            trace?.Invoke("TX " + Convert.ToHexString(frame));
            transport.Write(frame, 0, frame.Length);
            var response = new byte[3];
            ReadExact(response, 0, 3);
            if (request[1] is 3 or 4 && response[1] == request[1] && response[2] != request[5] * 2)
                throw new IOException("Invalid Modbus register count.");
            int length = (response[1] & 128) != 0 ? 5 : request[1] is 3 or 4 ? 5 + response[2] : 8;
            var bytes = new byte[length];
            response.CopyTo(bytes, 0);
            ReadExact(bytes, 3, length - 3);
            trace?.Invoke("RX " + Convert.ToHexString(bytes));
            if (Crc(bytes.AsSpan(0, bytes.Length - 2)) != (ushort)(bytes[^2] | bytes[^1] << 8)) throw new IOException("Modbus CRC mismatch.");
            if (bytes[0] != request[0]) throw new IOException("Unexpected Modbus slave.");
            if (bytes[1] == (request[1] | 128)) throw new IOException($"Modbus exception {bytes[2]}.");
            if (bytes[1] != request[1]) throw new IOException("Unexpected Modbus function.");
            if (request[1] is 6 or 16 && !bytes.AsSpan(2, 4).SequenceEqual(request.AsSpan(2, 4))) throw new IOException("Invalid Modbus write acknowledgement.");
            return bytes;
        }
    }
    /// <summary>Accumulates fragmented transport reads and traces partial responses before propagating I/O and timeout failures.</summary>
    private void ReadExact(byte[] buffer, int offset, int count)
    {
        int received = 0;
        try
        {
            while (received < count)
            {
                int read = transport.Read(buffer, offset + received, count - received);
                if (read == 0) throw new EndOfStreamException("Incomplete Modbus response.");
                received += read;
            }
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            trace?.Invoke($"RX partial {Convert.ToHexString(buffer.AsSpan(offset, received))}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
    /// <summary>Computes the Modbus CRC-16; RTU transmits its low byte before its high byte.</summary>
    internal static ushort Crc(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xffff;
        foreach (byte value in bytes) { crc ^= value; for (int i = 0; i < 8; i++) crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xa001 : crc >> 1); }
        return crc;
    }
    /// <summary>Leaves the caller-owned transport open; the register client has no transport resource to release.</summary>
    public void Dispose() { } // The caller owns the serial port.
}


