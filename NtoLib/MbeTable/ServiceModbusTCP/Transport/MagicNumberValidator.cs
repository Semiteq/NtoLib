using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using EasyModbus;
using EasyModbus.Exceptions;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.MbeTable.ServiceModbusTCP.Errors;

namespace NtoLib.MbeTable.ServiceModbusTCP.Transport;

internal sealed class MagicNumberValidator
{
	private readonly ILogger<MagicNumberValidator> _logger;

	public MagicNumberValidator(ILogger<MagicNumberValidator> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Result> ValidateAsync(
		ModbusClient client,
		RuntimeOptions settings,
		string reason,
		CancellationToken ct)
	{
		if (client is null)
			throw new ArgumentNullException(nameof(client));
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		_logger.LogTrace("Validating PLC magic number, reason: {Reason}", reason);

		try
		{
			using var _ = MetricsStopwatch.Start("Validate magic", _logger);

			var registers = await Task.Run(
				() => client.ReadHoldingRegisters(settings.ControlRegister, 1),
				ct).ConfigureAwait(false);

			var magicValue = registers[0];
			_logger.LogTrace("Read magic value: {Value}, expected {Expected}", magicValue, settings.MagicNumber);

			if (magicValue != settings.MagicNumber)
			{
				return Result.Fail(new ModbusTcpInvalidResponseError(settings.MagicNumber, magicValue));
			}

			return Result.Ok();
		}
		catch (Exception ex) when (ex is IOException or SocketException or ConnectionException)
		{
			_logger.LogError(ex, "Communication error during PLC validation");
			return Result.Fail(new ModbusTcpTimeoutError("magic validation", settings.TimeoutMs)).WithError(ex.Message);
		}
		catch (ModbusException mex)
		{
			_logger.LogError(mex, "PLC validation failed");
			return Result.Fail(new ModbusTcpReadFailedError(settings.ControlRegister, 1, mex.Message))
				.WithError(mex.Message);
		}
	}
}
