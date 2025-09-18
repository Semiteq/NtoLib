#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;

public sealed class PlcProtocol : IPlcProtocol
{
    private const int MaxChunkSize = 123;

    private readonly IModbusTransport _modbusTransport;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;

    private const int OffsetNumberLines = 1;

    public PlcProtocol(
        IModbusTransport modbusTransport,
        ICommunicationSettingsProvider communicationSettingsProvider,
        ILogger debugLogger)
    {
        _modbusTransport = modbusTransport ?? throw new ArgumentNullException(nameof(modbusTransport));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();

    public Result WriteAllAreas(int[] intData, int[] floatData, int rowCount)
    {
        if (intData.Length > 0)
        {
            _debugLogger.Log("Writing int data...");
            var writeResult = _modbusTransport.WriteMultipleRegistersChunked(Settings.IntBaseAddr, intData, MaxChunkSize);
            if (writeResult.IsFailed)
                return writeResult;
        }

        if (floatData.Length > 0)
        {
            _debugLogger.Log("Writing float data...");
            var writeResult = _modbusTransport.WriteMultipleRegistersChunked(Settings.FloatBaseAddr, floatData, MaxChunkSize);
            if (writeResult.IsFailed)
                return writeResult;
        }

        var writeRowCountResult = _modbusTransport.WriteSingleRegister(Settings.ControlBaseAddr + OffsetNumberLines, rowCount);
        if (writeRowCountResult.IsFailed)
            return writeRowCountResult;

        return Result.Ok();
    }

    public Result<int> ReadRowCount()
    {
        var rowCountResult = _modbusTransport.ReadHoldingRegisters(Settings.ControlBaseAddr + OffsetNumberLines, 1);
        if (rowCountResult.IsFailed)
            return rowCountResult.ToResult<int>();
        
        var rowCount = rowCountResult.Value[0];
        if (rowCount < 0)
            return Result.Fail("Invalid recipe row count in PLC.");
        
        return Result.Ok(rowCount);
    }

    public Result<int[]> ReadIntArea(int registerCount)
    {
        if (registerCount <= 0)
            return Result.Ok(Array.Empty<int>());

        return _modbusTransport.ReadHoldingRegistersChunked(Settings.IntBaseAddr, registerCount, MaxChunkSize);
    }

    public Result<int[]> ReadFloatArea(int registerCount)
    {
        if (registerCount <= 0)
            return Result.Ok(Array.Empty<int>());

        return _modbusTransport.ReadHoldingRegistersChunked(Settings.FloatBaseAddr, registerCount, MaxChunkSize);
    }
}