using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using EasyModbus;
using EasyModbus.Exceptions;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

using Polly;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ModbusTransport : IModbusTransport, IDisposable
{
    private readonly IRuntimeOptionsProvider _optionsProvider;
    private readonly ILogger<ModbusTransport> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private AsyncPolicy? _connectRetryPolicy;
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
                var validateRes = await ValidateMagicNumberAsync(settings, ct).ConfigureAwait(false);
                if (validateRes.IsSuccess)
                    return Result.Ok();

                DisconnectInternal();
            }

            _connectRetryPolicy = PollyPolicyFactory.Create(settings.MaxRetries, settings.BackoffDelayMs, _logger);
            _operationRetryPolicy = PollyPolicyFactory.CreateOperationPolicy(settings.MaxRetries, settings.BackoffDelayMs, _logger);

            InitializeClient(settings);
            _lastConnectionString = currentConnectionString;

            return await _connectRetryPolicy
                .ExecuteAsync(_ => ConnectAndValidateAsync(settings, ct), ct)
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
            return await ExecuteWithRetryAsync(
                () => _client!.ReadHoldingRegisters(address, length),
                length,
                "Read",
                ct).ConfigureAwait(false);
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

    private void InitializeClient(RuntimeOptions settings)
    {
        _client = new ModbusClient(settings.IpAddress.ToString(), settings.Port)
        {
            UnitIdentifier = settings.UnitId,
            ConnectionTimeout = settings.TimeoutMs
        };
    }

    private async Task<Result> ConnectAndValidateAsync(RuntimeOptions settings, CancellationToken ct)
    {
        try
        {
            await Task.Run(_client!.Connect, ct).ConfigureAwait(false);
            _logger.LogDebug("Connected to PLC {Ip}:{Port}", settings.IpAddress, settings.Port);

            return await ValidateMagicNumberAsync(settings, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            DisconnectInternal();
            _logger.LogError(ex, "Communication error during PLC connection");
            return Fail(Codes.PlcFailedToPing, ex.Message);
        }
        catch (ModbusException mex)
        {
            DisconnectInternal();
            _logger.LogError(mex, "PLC connection failed");
            return Fail(Codes.PlcFailedToPing, mex.Message);
        }
    }

    private async Task<Result> ValidateMagicNumberAsync(RuntimeOptions settings, CancellationToken ct)
    {
        _logger.LogTrace("Validating PLC magic number");
        try
        {
            var res = await ExecuteDirectAsync(
                () => _client!.ReadHoldingRegisters(settings.ControlRegister, 1),
                1, "Validate", ct).ConfigureAwait(false);

            if (res.IsFailed)
                return res.ToResult();

            var magicValue = res.Value[0];
            _logger.LogTrace("Read magic value: {Value}, expected {Expected}", magicValue, settings.MagicNumber);

            if (magicValue != settings.MagicNumber)
            {
                return Fail(Codes.PlcInvalidResponse,
                    $"Control register validation failed. Expected {settings.MagicNumber}, got {magicValue}");
            }

            return Result.Ok();
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            DisconnectInternal();
            _logger.LogError(ex, "Communication error during PLC validation");
            return Fail(Codes.PlcTimeout, ex.Message);
        }
        catch (ModbusException mex)
        {
            DisconnectInternal();
            _logger.LogError(mex, "PLC validation failed");
            return Fail(Codes.PlcReadFailed, mex.Message);
        }
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
            _logger.LogError(ex, "Communication error during PLC operation");
            return Fail(Codes.PlcTimeout, ex.Message).ToResult<T>();
        }
        catch (ModbusException mex)
        {
            DisconnectInternal();
            _logger.LogError(mex, "PLC operation failed");
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