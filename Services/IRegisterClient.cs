namespace OBLSequenceStudio.Services;

internal interface IRegisterClient : IDisposable
{
    ushort[] ReadHoldingRegisters(byte slave, ushort address, ushort count);
    ushort[] ReadInputRegisters(byte slave, ushort address, ushort count);
    void WriteSingleRegister(byte slave, ushort address, ushort value);
    void WriteMultipleRegisters(byte slave, ushort address, ushort[] values);
}
