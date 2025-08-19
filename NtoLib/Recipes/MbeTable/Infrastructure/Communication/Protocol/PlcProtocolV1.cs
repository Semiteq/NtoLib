#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Protocol;

public sealed class PlcProtocolV1 : IPlcProtocol
{
    private const int MaxChunkSize = 123;

    private readonly IModbusTransport _modbusTransport;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;

    public PlcProtocolV1(IModbusTransport modbusTransport, ICommunicationSettingsProvider communicationSettingsProvider, ILogger debugLogger)
    {
        _modbusTransport = modbusTransport ?? throw new ArgumentNullException(nameof(modbusTransport));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public Result CheckConnection()
    {
        try
        {
            if (Settings.IsRecipeActive)
                return Result.Fail($"Рецепт уже исполняется на ПЛК");
            return _modbusTransport.CheckConnection();
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Connection check error: {ex.Message}");
            return Result.Fail("Ошибка при проверке соединения с ПЛК.");
        }
    }

    public Result WriteAllAreas(int[] intData, int[] floatData, int[] boolData, int rowCount)
    {
        try
        {
            var connectionResult = _modbusTransport.Connect();
            if (connectionResult.IsFailed)
                return Result.Fail(connectionResult.Errors);

            if (intData.Length > 0)
            {
                _debugLogger.Log($"Writing int data: [{string.Join(",", intData)}]");
                _modbusTransport.WriteMultipleRegistersChunked(Settings.IntBaseAddr, intData, MaxChunkSize);
            }

            if (floatData.Length > 0)
            {
                _debugLogger.Log($"Writing float data: [{string.Join(",", floatData)}]");
                _modbusTransport.WriteMultipleRegistersChunked(Settings.FloatBaseAddr, floatData, MaxChunkSize);
            }

            if (boolData.Length > 0)
            {
                _debugLogger.Log($"Writing bool data: [{string.Join(",", boolData)}]");
                _modbusTransport.WriteMultipleRegistersChunked(Settings.BoolBaseAddr, boolData, MaxChunkSize);
            }

            // LineCount
            _modbusTransport.WriteSingleRegister(Settings.ControlBaseAddr + 2, rowCount);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Write error: {ex.Message}");
            return Result.Fail("Ошибка при записи данных рецепта в ПЛК.");
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }

    public Result<(int[] IntData, int[] FloatData, int RowCount)> ReadAllAreas()
    {
        try
        {
            var connectionResult = _modbusTransport.Connect();
            if (connectionResult.IsFailed)
                return Result.Fail(connectionResult.Errors);

            var rowCount = _modbusTransport.ReadHoldingRegisters(Settings.ControlBaseAddr + 2, 1)[0];

            if (rowCount <= 0)
                return Result.Fail<(int[], int[], int)>("Некорректное число строк рецепта в ПЛК.");

            var intQty = rowCount * Settings.IntColumNum;
            var floatQty = rowCount * Settings.FloatColumNum * 2;
            var boolQty = rowCount * Settings.BoolColumNum;

            var ints = intQty > 0
                ? _modbusTransport.ReadHoldingRegistersChunked(Settings.IntBaseAddr, intQty, MaxChunkSize)
                : Array.Empty<int>();

            var floats = floatQty > 0
                ? _modbusTransport.ReadHoldingRegistersChunked(Settings.FloatBaseAddr, floatQty, MaxChunkSize)
                : Array.Empty<int>();

            _ = boolQty;

            return Result.Ok((ints, floats, rowCount));
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Read error: {ex.Message}");
            return Result.Fail<(int[], int[], int)>("Ошибка при чтении данных рецепта из ПЛК.");
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }
}