using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using EasyModbus.Exceptions;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.MbeTable.ServiceModbusTCP.Errors;

using Polly;

namespace NtoLib.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ModbusTransport : IModbusTransport
{
	private readonly ModbusConnectionManager _connectionManager;
	private readonly IRuntimeOptionsProvider _optionsProvider;
	private readonly ILogger<ModbusTransport> _logger;
	private readonly SemaphoreSlim _operationLock = new(1, 1);

	private AsyncPolicy? _operationRetryPolicy;

	public ModbusTransport(
		ModbusConnectionManager connectionManager,
		IRuntimeOptionsProvider optionsProvider,
		ILogger<ModbusTransport> logger)
	{
		_connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
		_optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Result> EnsureConnectedAsync(CancellationToken ct)
	{
		var result = await _connectionManager.EnsureConnectedAsync(ct).ConfigureAwait(false);

		if (result.IsSuccess && _operationRetryPolicy == null)
		{
			_operationRetryPolicy = PollyPolicyFactory.CreateOperationPolicy(3, 200, _logger);
		}

		return result;
	}

	public async Task<Result<int[]>> ReadHoldingAsync(int address, int length, CancellationToken ct)
	{
		await _operationLock.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			return await ExecuteWithRetryAsync(
				() => _connectionManager.Client!.ReadHoldingRegisters(address, length),
				address,
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
				_connectionManager.Client!.WriteMultipleRegisters(address, data);
				return true;
			}, address, data.Length, "Write", ct).ConfigureAwait(false);

			return res.ToResult();
		}
		finally
		{
			_operationLock.Release();
		}
	}

	public void Disconnect() => _connectionManager.Disconnect("manual");

	public void Dispose()
	{
		_connectionManager.Dispose();
		_operationLock.Dispose();
	}

	private async Task<Result<T>> ExecuteWithRetryAsync<T>(
		Func<T> operation,
		int address,
		int size,
		string opName,
		CancellationToken ct)
	{
		var opContext = new OperationContext(opName, address, size, _connectionManager.CurrentConnectionId);

		using var _ = MetricsStopwatch.Start($"{opName} {size} reg", _logger);

		var ensure = await EnsureConnectedAsync(ct).ConfigureAwait(false);
		if (ensure.IsFailed)
		{
			_logger.LogError(
				"Operation [{OperationId}] failed during ensure connected: {Type} addr={Address} size={Size}",
				opContext.OperationId, opContext.Type, opContext.Address, opContext.Size);
			return ensure.ToResult<T>();
		}

		if (_operationRetryPolicy != null)
		{
			return await _operationRetryPolicy
				.ExecuteAsync(_ => ExecuteOperationAsync(operation, opContext, ct), ct)
				.ConfigureAwait(false);
		}

		return await ExecuteOperationAsync(operation, opContext, ct).ConfigureAwait(false);
	}

	private async Task<Result<T>> ExecuteOperationAsync<T>(
		Func<T> operation,
		OperationContext opContext,
		CancellationToken ct)
	{
		try
		{
			var result = await Task.Run(operation, ct).ConfigureAwait(false);
			return Result.Ok(result);
		}
		catch (Exception ex) when (ex is IOException or SocketException or ConnectionException)
		{
			_connectionManager.Disconnect("operation_error");
			_logger.LogError(ex,
				"Communication error [{OperationId}]: {Type} addr={Address} size={Size} conn=[{ConnectionId}] exception=[{ExceptionType}]",
				opContext.OperationId, opContext.Type, opContext.Address, opContext.Size,
				opContext.ConnectionId, ex.GetType().Name);

			var settings = _optionsProvider.GetCurrent();
			return Result.Fail(new ModbusTcpTimeoutError(opContext.Type, settings.TimeoutMs)).WithError(ex.Message)
				.ToResult<T>();
		}
		catch (ModbusException mex)
		{
			_connectionManager.Disconnect("operation_error");
			_logger.LogError(mex,
				"PLC operation failed [{OperationId}]: {Type} addr={Address} size={Size} conn=[{ConnectionId}]",
				opContext.OperationId, opContext.Type, opContext.Address, opContext.Size,
				opContext.ConnectionId);

			return opContext.Type == "Read"
				? Result.Fail(new ModbusTcpReadFailedError(opContext.Address, opContext.Size, mex.Message))
					.ToResult<T>()
				: Result.Fail(new ModbusTcpFailedError(opContext.Address, opContext.Size, mex.Message)).ToResult<T>();
		}
	}
}
