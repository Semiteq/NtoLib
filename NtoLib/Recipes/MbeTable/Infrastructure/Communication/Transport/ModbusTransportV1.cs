#nullable enable

using System;
using EasyModbus;
using FluentResults;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Transport;

public sealed class ModbusTransportV1 : IModbusTransport
{
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;
    private ModbusClient? _modbusClient;

    private const int AvailabilityWaitSpan = 100; //[ms]
    
    public ModbusTransportV1(ICommunicationSettingsProvider communicationSettingsProvider, ILogger logger)
    {
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public Result CheckConnection()
    {
        try
        {
            Connect();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Check connection failed: {ex.Message}");
            return Result.Fail("Ошибка при подключении к ПЛК.");
        }
        finally
        {
            TryDisconnect();
        }
    }

    public Result Connect()
    {
        if (_modbusClient is { Connected: true }) 
            return Result.Ok();
        
        var ip = $"{Settings.Ip1}.{Settings.Ip2}.{Settings.Ip3}.{Settings.Ip4}";
        
        _modbusClient = new ModbusClient(ip, Settings.Port);
        
        _debugLogger.Log($"Connecting to {ip}:{Settings.Port}");
        
        _modbusClient.Connect();
        if (!_modbusClient.Available(AvailabilityWaitSpan))
        {
            _debugLogger.Log($"Didn't recive availability message after {AvailabilityWaitSpan} ms");
            return Result.Fail($"Соединение установлено но тестовое сообщение было потеряно.");
        }
        return Result.Ok();
    }

    public void TryDisconnect()
    {
        try
        {
            if (_modbusClient is { Connected: true })
            {
                _modbusClient.Disconnect();
                _debugLogger.Log("Disconnected");
            }
        }
        catch
        {
        }
        finally
        {
            _modbusClient = null;
        }
    }

    public void WriteSingleRegister(int address, int value)
    {
        if (_modbusClient is null) 
            throw new InvalidOperationException("Not connected");
        _modbusClient.WriteSingleRegister(address, value);
    }

    public void WriteMultipleRegistersChunked(int baseAddress, int[] values, int chunkMax)
    {
        if (_modbusClient is null) 
            throw new InvalidOperationException("Not connected");
        var total = values.Length;
        var index = 0;
        while (index < total)
        {
            var size = Math.Min(chunkMax, total - index);
            var chunk = new int[size];
            Array.Copy(values, index, chunk, 0, size);
            _modbusClient.WriteMultipleRegisters(baseAddress + index, chunk);
            index += size;
        }
    }

    public int[] ReadHoldingRegisters(int address, int length)
    {
        if (_modbusClient is null) 
            throw new InvalidOperationException("Not connected");
        return _modbusClient.ReadHoldingRegisters(address, length);
    }

    public int[] ReadHoldingRegistersChunked(int baseAddress, int totalRegisters, int chunkMax)
    {
        if (_modbusClient is null) 
            throw new InvalidOperationException("Not connected");
        var result = new int[totalRegisters];
        var index = 0;
        while (index < totalRegisters)
        {
            var size = Math.Min(chunkMax, totalRegisters - index);
            var chunk = _modbusClient.ReadHoldingRegisters(baseAddress + index, size);
            Array.Copy(chunk, 0, result, index, size);
            index += size;
        }
        return result;
    }
}