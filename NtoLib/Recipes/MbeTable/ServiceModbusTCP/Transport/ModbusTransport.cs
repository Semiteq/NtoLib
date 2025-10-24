using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using EasyModbus;
using EasyModbus.Exceptions;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

using Polly;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ModbusTransport : IModbusTransport
{
    private readonly IRuntimeOptionsProvider _optionsProvider;
    private readonly ILogger<ModbusTransport> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private AsyncPolicy? _retryPolicy;
    private AsyncPolicy? _operationRetryPolicy;
    private ModbusClient? _client;
    private string? _lastConnectionString;

    public ModbusTransport(
        IRuntimeOptionsProvider optionsProvider,
        ILogger<ModbusTransport> logger)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> EnsureConnectedAsync(CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var settings = _optionsProvider.GetCurrent();
            var currentConnectionString = $"{settings.IpAddress}:{settings.Port}:{settings.UnitId}";

            if (_lastConnectionString != null &&
                _lastConnectionString != currentConnectionString)
            {
                _logger.LogDebug("Connection parameters changed, reconnecting");
                DisconnectInternal();
            }

            if (_client?.Connected == true)
            {
                var pingRes = await PingDirectAsync(settings, ct).ConfigureAwait(false);
                if (pingRes.IsSuccess)
                    return Result.Ok();

                DisconnectInternal();
            }

            _retryPolicy = PollyPolicyFactory.Create(settings.MaxRetries, settings.BackoffDelayMs, _logger);
            _operationRetryPolicy = PollyPolicyFactory.CreateOperationPolicy(2, settings.BackoffDelayMs, _logger);

            _client = new ModbusClient(settings.IpAddress.ToString(), settings.Port)
            {
                UnitIdentifier = settings.UnitId,
                ConnectionTimeout = settings.TimeoutMs
            };

            _lastConnectionString = currentConnectionString;

            return await _retryPolicy
                .ExecuteAsync(_ => ConnectAndPingAsync(settings, ct), ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<Result<int[]>> ReadHoldingAsync(int address, int length, CancellationToken ct)
    {
        await _operationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var res = await ExecuteWithRetryAsync(
                () => _client!.ReadHoldingRegisters(address, length), 
                length, 
                "Read", 
                ct).ConfigureAwait(false);
        
            if (res.IsSuccess)
            {
                var preview = res.Value.Length <= 20 
                    ? res.Value 
                    : res.Value.Take(20).ToArray();
                _logger.LogTrace("Read from {address} holding result: [{Values}]", 
                    address, string.Join(", ", preview));
            }

            return res;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<Result> WriteHoldingAsync(int address, int[] data, CancellationToken ct)
    {
        await _operationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var res = await ExecuteWithRetryAsync(() =>
            {
                _client!.WriteMultipleRegisters(address, data);
                return true;
            }, data.Length, "Write", ct).ConfigureAwait(false);

            return res.ToResult();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public void Disconnect() => DisconnectInternal();

    public void Dispose()
    {
        DisconnectInternal();
        _connectionLock.Dispose();
        _operationLock.Dispose();
    }

    private async Task<Result> ConnectAndPingAsync(RuntimeOptions settings, CancellationToken ct)
    {
        try
        {
            await Task.Run(_client!.Connect, ct).ConfigureAwait(false);
            _logger.LogDebug("Connected to PLC {Ip}:{Port}", settings.IpAddress, settings.Port);

            return await PingDirectAsync(settings, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            DisconnectInternal();
            return Fail(Codes.PlcConnectionFailed, ex.Message);
        }
        catch (ModbusException mex)
        {
            DisconnectInternal();
            return Fail(Codes.PlcConnectionFailed, mex.Message);
        }
    }

    private async Task<Result> PingDirectAsync(RuntimeOptions settings, CancellationToken ct)
    {
        _logger.LogTrace("Pinging PLC");
        
        var res = await ExecuteDirectAsync(
            () => _client!.ReadHoldingRegisters(settings.ControlRegister, 1),
            1, "Ping", ct).ConfigureAwait(false);
        
        if (res.IsFailed)
            return res.ToResult();

        var magicValue = res.Value[0];
        _logger.LogTrace("Read magic value: {Value}, expecting {Expecting}", magicValue, settings.MagicNumber);
        
        if (magicValue != settings.MagicNumber)
        {
            return Fail(Codes.PlcInvalidResponse,
                $"Control register validation failed. Expected {settings.MagicNumber}, got {magicValue}");
        }

        return Result.Ok();
    }

    private async Task<Result<T>> ExecuteWithRetryAsync<T>(
        Func<T> operation,
        int size,
        string opName,
        CancellationToken ct)
    {
        using var _ = MetricsStopwatch.Start($"{opName} {size} reg", _logger);

        var ensure = await EnsureConnectedAsync(ct).ConfigureAwait(false);
        if (ensure.IsFailed)
            return ensure.ToResult<T>();

        if (_operationRetryPolicy != null)
        {
            return await _operationRetryPolicy
                .ExecuteAsync(_ => ExecuteOperationAsync(operation, ct), ct)
                .ConfigureAwait(false);
        }

        return await ExecuteOperationAsync(operation, ct).ConfigureAwait(false);
    }

    private async Task<Result<T>> ExecuteDirectAsync<T>(
        Func<T> operation,
        int size,
        string opName,
        CancellationToken ct)
    {
        using var _ = MetricsStopwatch.Start($"{opName} {size} reg", _logger);
        return await ExecuteOperationAsync(operation, ct).ConfigureAwait(false);
    }

    private async Task<Result<T>> ExecuteOperationAsync<T>(Func<T> operation, CancellationToken ct)
    {
        try
        {
            var result = await Task.Run(operation, ct).ConfigureAwait(false);
            return Result.Ok(result);
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            DisconnectInternal();
            return Fail(Codes.PlcTimeout, ex.Message).ToResult<T>();
        }
        catch (ModbusException mex)
        {
            return Fail(Codes.PlcReadFailed, mex.Message).ToResult<T>();
        }
    }

    private void DisconnectInternal()
    {
        try
        {
            if (_client?.Connected == true)
            {
                _client.Disconnect();
                _logger.LogDebug("PLC disconnected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error on disconnect: {Message}", ex.Message);
        }
        finally
        {
            _client = null;
            _lastConnectionString = null;
        }
    }

    private static Result Fail(Codes code, string message) =>
        Result.Fail(new Error(message).WithMetadata(nameof(Codes), code));
}