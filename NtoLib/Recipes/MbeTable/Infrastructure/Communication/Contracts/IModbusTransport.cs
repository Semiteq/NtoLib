#nullable enable
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

public interface IModbusTransport
{
    Result CheckConnection();
    Result Connect();
    void TryDisconnect();
    void WriteSingleRegister(int address, int value);
    void WriteMultipleRegistersChunked(int baseAddress, int[] values, int chunkMax);
    int[] ReadHoldingRegisters(int address, int length);
    int[] ReadHoldingRegistersChunked(int baseAddress, int totalRegisters, int chunkMax);
}