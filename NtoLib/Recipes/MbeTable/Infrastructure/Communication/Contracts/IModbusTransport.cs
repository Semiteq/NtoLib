#nullable enable
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

public interface IModbusTransport
{
    Result Connect();
    void TryDisconnect();
    Result WriteSingleRegister(int address, int value);
    Result WriteMultipleRegistersChunked(int baseAddress, int[] values, int chunkMax);
    Result<int[]> ReadHoldingRegisters(int address, int length);
    Result<int[]> ReadHoldingRegistersChunked(int baseAddress, int totalRegisters, int chunkMax);
}