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

    public PlcProtocol(IModbusTransport modbusTransport, ICommunicationSettingsProvider communicationSettingsProvider, ILogger debugLogger)
    {
        _modbusTransport = modbusTransport ?? throw new ArgumentNullException(nameof(modbusTransport));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();

    public Result WriteAllAreas(int[] intData, int[] floatData, int[] boolData, int rowCount)
    {
        if (intData.Length > 0)
        {
            _debugLogger.Log($"Writing int data...");
            var writeResult = _modbusTransport.WriteMultipleRegistersChunked(Settings.IntBaseAddr, intData, MaxChunkSize);
            if (writeResult.IsFailed) return writeResult;
        }

        if (floatData.Length > 0)
        {
            _debugLogger.Log($"Writing float data...");
            var writeResult = _modbusTransport.WriteMultipleRegistersChunked(Settings.FloatBaseAddr, floatData, MaxChunkSize);
            if (writeResult.IsFailed) return writeResult;
        }

        if (boolData.Length > 0)
        {
            _debugLogger.Log($"Writing bool data...");
            var writeResult = _modbusTransport.WriteMultipleRegistersChunked(Settings.BoolBaseAddr, boolData, MaxChunkSize);
            if (writeResult.IsFailed) return writeResult;
        }

        var writeRowCountResult = _modbusTransport.WriteSingleRegister(Settings.ControlBaseAddr + 2, rowCount);
        if (writeRowCountResult.IsFailed) return writeRowCountResult;

        return Result.Ok();
    }

    public Result<(int[] IntData, int[] FloatData, int RowCount)> ReadAllAreas()
    {
        var rowCountReadingResult = _modbusTransport.ReadHoldingRegisters(Settings.ControlBaseAddr + 2, 1);
        if (rowCountReadingResult.IsFailed)
            return rowCountReadingResult.ToResult<(int[], int[], int)>();
        
        var rowCount = rowCountReadingResult.Value[0];

        if (rowCount == 0)
            return Result.Ok((Array.Empty<int>(), Array.Empty<int>(), 0));
        
        if (rowCount < 0)
            return Result.Fail<(int[], int[], int)>("Некорректное число строк рецепта в ПЛК.");

        var intQty = rowCount * Settings.IntColumNum;
        var floatQty = rowCount * Settings.FloatColumNum * 2;
        var boolQty = rowCount * Settings.BoolColumNum;

        var intReadingResult = Array.Empty<int>();
        if (intQty > 0)
        {
            var result = _modbusTransport.ReadHoldingRegistersChunked(Settings.IntBaseAddr, intQty, MaxChunkSize);
            if (result.IsFailed) return result.ToResult<(int[], int[], int)>();
            intReadingResult = result.Value;
        }

        var floatReadingResult = Array.Empty<int>();
        if (floatQty > 0)
        {
            var result = _modbusTransport.ReadHoldingRegistersChunked(Settings.FloatBaseAddr, floatQty, MaxChunkSize);
            if (result.IsFailed) return result.ToResult<(int[], int[], int)>();
            floatReadingResult = result.Value;
        }

        _ = boolQty;

        return Result.Ok((intReadingResult, floatReadingResult, rowCount));
    }
}