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
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

using Polly;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

public sealed class ModbusConnectionManager : IDisposable
{
	private const int DefaultStalenessMs = 0;
	private readonly SemaphoreSlim _connectionLock = new(1, 1);
	private readonly ILogger<ModbusConnectionManager> _logger;

	private readonly FbRuntimeOptionsProvider _optionsProvider;
	private readonly ConnectionStateTracker _stateTracker = new();
	private readonly MagicNumberValidator _validator;

	private AsyncPolicy? _connectRetryPolicy;
	private ConnectionContext? _currentContext;
	private string? _lastConnectionString;

	public ModbusConnectionManager(
		FbRuntimeOptionsProvider optionsProvider,
		MagicNumberValidator validator,
		ILogger<ModbusConnectionManager> logger)
	{
		_optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ModbusClient? Client { get; private set; }

	public Guid? CurrentConnectionId => _currentContext?.ConnectionId;

	public void Dispose()
	{
		DisconnectInternal("dispose");
		_connectionLock.Dispose();
	}

	public async Task<Result> EnsureConnectedAsync(CancellationToken ct)
	{
		await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var settings = _optionsProvider.GetCurrent();
			var currentConnectionString = $"{settings.IpAddress}:{settings.Port}:{settings.UnitId}";

			if (_lastConnectionString != null && _lastConnectionString != currentConnectionString)
			{
				_logger.LogDebug("Connection parameters changed, reconnecting");
				DisconnectInternal("params_changed");
			}

			var stalenessThreshold = TimeSpan.FromMilliseconds(DefaultStalenessMs);

			if (Client?.Connected == true)
			{
				if (_stateTracker.IsStale(stalenessThreshold))
				{
					_logger.LogDebug("Connection is stale, validating");
					var validateRes = await _validator
						.ValidateAsync(Client, settings, "stale_check", ct)
						.ConfigureAwait(false);

					if (validateRes.IsSuccess)
					{
						_stateTracker.MarkValidated();

						return Result.Ok();
					}

					_logger.LogWarning("Stale validation failed, reconnecting");
					DisconnectInternal("validation_failed");
				}
				else
				{
					_logger.LogTrace("Reusing existing connection [{ConnectionId}]", _currentContext?.ConnectionId);

					return Result.Ok();
				}
			}

			_connectRetryPolicy = PollyPolicyFactory.CreateConnectionPolicy(
				settings.MaxRetries,
				settings.BackoffDelayMs,
				_logger);

			InitializeClient(settings);
			_lastConnectionString = currentConnectionString;

			var reason = _currentContext == null ? "first_connect" : "reconnect";

			return await _connectRetryPolicy
				.ExecuteAsync(_ => ConnectAndValidateAsync(settings, currentConnectionString, reason, ct), ct)
				.ConfigureAwait(false);
		}
		finally
		{
			_connectionLock.Release();
		}
	}

	public void Disconnect(string reason = "manual")
	{
		DisconnectInternal(reason);
	}

	private void InitializeClient(RuntimeOptions settings)
	{
		Client = new ModbusClient(settings.IpAddress.ToString(), settings.Port)
		{
			UnitIdentifier = settings.UnitId,
			ConnectionTimeout = settings.TimeoutMs
		};
	}

	private async Task<Result> ConnectAndValidateAsync(
		RuntimeOptions settings,
		string connectionString,
		string reason,
		CancellationToken ct)
	{
		try
		{
			await Task.Run(Client!.Connect, ct).ConfigureAwait(false);

			_currentContext = new ConnectionContext(connectionString, reason);
			_stateTracker.Reset();

			_logger.LogDebug("Connected [{ConnectionId}] {Reason} to PLC {Ip}:{Port}",
				_currentContext.ConnectionId,
				reason,
				settings.IpAddress,
				settings.Port);

			var validateResult = await _validator
				.ValidateAsync(Client, settings, "on_connect", ct)
				.ConfigureAwait(false);

			if (validateResult.IsSuccess)
			{
				_stateTracker.MarkValidated();
				_logger.LogDebug("Magic number validation successful on connect.");

				return Result.Ok();
			}

			_logger.LogError("Magic number validation failed on connect: {Errors}. Disconnecting.",
				validateResult.Errors);
			DisconnectInternal("validation_failed");

			return validateResult;
		}
		catch (Exception ex) when (ex is IOException or SocketException or ConnectionException)
		{
			DisconnectInternal("connect_error");
			_logger.LogError(ex, "Communication error during PLC connection: {ExceptionType}", ex.GetType().Name);

			return Result.Fail(new ModbusTcpConnectionFailedError(
				settings.IpAddress.ToString(),
				settings.Port,
				ex));
		}
		catch (ModbusException mex)
		{
			DisconnectInternal("connect_error");
			_logger.LogError(mex, "PLC connection failed");

			return Result.Fail(new ModbusTcpConnectionFailedError(
				settings.IpAddress.ToString(),
				settings.Port,
				mex));
		}
	}

	private void DisconnectInternal(string reason)
	{
		try
		{
			if (Client?.Connected == true)
			{
				Client.Disconnect();
				_logger.LogDebug("PLC disconnected, reason: {Reason}, connection: [{ConnectionId}]",
					reason,
					_currentContext?.ConnectionId);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError("Error on disconnect: {Message}", ex.Message);
		}
		finally
		{
			Client = null;
			_lastConnectionString = null;
			_currentContext = null;
			_stateTracker.Reset();
		}
	}
}
