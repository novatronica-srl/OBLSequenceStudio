namespace OBLSequenceStudio.Services;

/// <summary>Register-level Modbus contract used by board workflows and replaceable transports.</summary>
internal interface IRegisterClient : IDisposable
{
    /// <summary>Reads holding register words at a zero-based protocol address.</summary>
    ushort[] ReadHoldingRegisters(byte slave, ushort address, ushort count);
    /// <summary>Reads read-only input register words at a zero-based protocol address.</summary>
    ushort[] ReadInputRegisters(byte slave, ushort address, ushort count);
    /// <summary>Writes one holding register word to the addressed slave.</summary>
    void WriteSingleRegister(byte slave, ushort address, ushort value);
    /// <summary>Writes consecutive holding register words to the addressed slave.</summary>
    void WriteMultipleRegisters(byte slave, ushort address, ushort[] values);
}
