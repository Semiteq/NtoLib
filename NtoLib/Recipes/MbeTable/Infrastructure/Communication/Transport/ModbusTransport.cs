#nullable enable

using System;
using System.IO;
using System.Net.Sockets;
using EasyModbus;
using EasyModbus.Exceptions;
using FluentResults;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Transport;

/// <summary>
/// Provides a robust, thread-safe implementation for Modbus TCP communication.
/// This class incorporates retry logic, proper exception handling, and resource management
/// to ensure reliable data transmission or a clear failure status.
/// </summary>
public sealed class ModbusTransport : IModbusTransport, IDisposable
{
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;
    private readonly object _lock = new();

    private ModbusClient? _modbusClient;

    // These values should ideally come from settings for flexibility.
    private const int TimeoutMs = 1000;
    private const int MaxRetries = 2;
    
    // Unique id for connectivity check, also used as unit id for Modbus TCP
    private const byte UnitId = 69; 
    private const int ControlRegisterNumber = 69;

    public ModbusTransport(ICommunicationSettingsProvider communicationSettingsProvider, ILogger logger)
    {
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public Result Connect()
    {
        lock (_lock)
        {
            if (_modbusClient is { Connected: true })
            {
                return Result.Ok();
            }

            // Attempt to disconnect and clean up any previous state before reconnecting.
            DisconnectInternal();

            try
            {
                var ip = $"{Settings.Ip1}.{Settings.Ip2}.{Settings.Ip3}.{Settings.Ip4}";
                _modbusClient = new ModbusClient(ip, Settings.Port)
                {
                    ConnectionTimeout = TimeoutMs,
                    UnitIdentifier = UnitId
                };
                
                _debugLogger.Log($"Connecting to {ip}:{Settings.Port} with a {TimeoutMs}ms timeout...");
                _modbusClient.Connect();
                _debugLogger.Log("Connection successful.");

                var controlregNumber = _modbusClient.ReadHoldingRegisters(Settings.ControlBaseAddr, 1)[0];
                if (controlregNumber == ControlRegisterNumber)
                {
                    _debugLogger.Log("Connection validated with a test read.");
                    return Result.Ok();
                }
                else
                {
                    _debugLogger.Log("Connection validation failed. Unexpected control register value.");
                    return Result.Fail("Connection validation failed. Could not read the expected control register value.");
                }
            }
            catch (Exception ex)
            {
                DisconnectInternal(); // Ensure cleanup on failure
                var errorMessage = $"Failed to connect or validate connection: {ex.Message}";
                _debugLogger.Log(errorMessage);
                return Result.Fail(errorMessage);
            }
        }
    }
    
    public void TryDisconnect()
    {
        lock (_lock)
        {
            DisconnectInternal();
        }
    }
    
    public Result WriteSingleRegister(int address, int value)
    {
        return ExecuteWithRetry(client =>
        {
            client.WriteSingleRegister(address, value);
            _debugLogger.Log($"Successfully wrote value {value} to register {address}.");
        });
    }
    
    public Result WriteMultipleRegistersChunked(int baseAddress, int[] values, int chunkMax)
    {
        return ExecuteWithRetry(client =>
        {
            var total = values.Length;
            var index = 0;
            while (index < total)
            {
                var size = Math.Min(chunkMax, total - index);
                var chunk = new int[size];
                Array.Copy(values, index, chunk, 0, size);

                // The operation for each chunk must be retried individually if it fails,
                // but for simplicity, we retry the whole sequence.
                // For transactional integrity, a more complex logic would be needed.
                client.WriteMultipleRegisters(baseAddress + index, chunk);
                _debugLogger.Log($"Successfully wrote chunk of {size} registers at base address {baseAddress + index}.");
                index += size;
            }
        });
    }
    
    public Result<int[]> ReadHoldingRegisters(int address, int length)
    {
        return ExecuteWithRetry(client => client.ReadHoldingRegisters(address, length));
    }
    
    public Result<int[]> ReadHoldingRegistersChunked(int baseAddress, int totalRegisters, int chunkMax)
    {
        return ExecuteWithRetry(client =>
        {
            var result = new int[totalRegisters];
            var index = 0;
            while (index < totalRegisters)
            {
                var size = Math.Min(chunkMax, totalRegisters - index);
                var chunk = client.ReadHoldingRegisters(baseAddress + index, size);
                Array.Copy(chunk, 0, result, index, size);
                index += size;
            }
            return result;
        });
    }

    /// <summary>
    /// Releases the managed resources, ensuring the Modbus connection is closed.
    /// </summary>
    public void Dispose()
    {
        TryDisconnect();
    }

    /// <summary>
    /// Centralized method for executing Modbus read operations with built-in retry logic.
    /// </summary>
    private Result<T> ExecuteWithRetry<T>(Func<ModbusClient, T> modbusFunc)
{
    lock (_lock)
    {
        if (_modbusClient is null)
        {
            return Result.Fail("Modbus client is not initialized. Call Connect() first.");
        }
        
        for (var i = 0; i < MaxRetries; i++)
        {
            try
            {
                return Result.Ok(modbusFunc(_modbusClient));
            }
            catch (Exception ex) when (ex is IOException or SocketException or NullReferenceException)
            {
                _debugLogger.Log($"Attempt {i + 1}/{MaxRetries} failed due to a network or client error: {ex.Message}");
                DisconnectInternal();
                
                if (i < MaxRetries - 1) 
                {
                    System.Threading.Thread.Sleep(100); 
                }
            }
            catch (ModbusException modbusEx)
            {
                var errorMessage = $"PLC returned a Modbus exception: {modbusEx.Message}";
                _debugLogger.Log(errorMessage);
                return Result.Fail(errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"An unexpected error occurred during Modbus operation: {ex.Message}";
                _debugLogger.Log($"{errorMessage}\n{ex.StackTrace}");
                DisconnectInternal();
                return Result.Fail(errorMessage);
            }
        }

        var finalError = $"Operation failed after {MaxRetries} attempts.";
        _debugLogger.Log(finalError);
        return Result.Fail(finalError);
    }
}

    /// <summary>
    /// Centralized method for executing Modbus write operations with built-in retry logic.
    /// </summary>
    private Result ExecuteWithRetry(Action<ModbusClient> modbusAction)
    {
        // This is a wrapper around the generic version for non-return actions.
        var result = ExecuteWithRetry<bool>(client =>
        {
            modbusAction(client);
            return true; // Dummy return value
        });

        return result.IsSuccess ? Result.Ok() : result.ToResult();
    }

    /// <summary>
    /// Safely disconnects and cleans up the ModbusClient instance.
    /// This method does not lock; it should be called from within a lock.
    /// </summary>
    private void DisconnectInternal()
    {
        if (_modbusClient is null) return;
        
        try
        {
            if (_modbusClient.Connected)
            {
                _modbusClient.Disconnect();
                _debugLogger.Log("Disconnected.");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"An error occurred during disconnection: {ex.Message}");
        }
        finally
        {
            _modbusClient = null;
        }
    }
}