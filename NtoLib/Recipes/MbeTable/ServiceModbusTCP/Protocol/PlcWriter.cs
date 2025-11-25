using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

internal sealed class PlcWriter : IPlcWriter
{
	private const int ChunkSize = 123;
	private const int OffsetRowCount = 1;

	private readonly IModbusTransport _transport;
	private readonly IModbusChunkHandler _chunkHandler;
	private readonly IRuntimeOptionsProvider _optionsProvider;
	private readonly ILogger<PlcWriter> _logger;

	public PlcWriter(
		IModbusTransport transport,
		IModbusChunkHandler chunkHandler,
		IRuntimeOptionsProvider optionsProvider,
		ILogger<PlcWriter> logger)
	{
		_transport = transport ?? throw new ArgumentNullException(nameof(transport));
		_chunkHandler = chunkHandler ?? throw new ArgumentNullException(nameof(chunkHandler));
		_optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Result> WriteAllAreasAsync(
		int[] intData, int[] floatData, int rowCount, CancellationToken ct)
	{
		using var _ = MetricsStopwatch.Start("WriteAllAreas", _logger);

		var settings = _optionsProvider.GetCurrent();

		_logger.LogTrace("Int data length: {Length}", intData.Length);
		if (intData.Length > 0)
		{
			_logger.LogTrace("Writing {Count} int registers to address {Address}",
				intData.Length, settings.IntBaseAddr);

			var result = await _chunkHandler.WriteChunkedAsync(
					_transport, settings.IntBaseAddr, intData, ChunkSize, ct)
				.ConfigureAwait(false);

			if (result.IsFailed)
				return result;
		}

		_logger.LogTrace("Float data length: {Length}", floatData.Length);
		if (floatData.Length > 0)
		{
			_logger.LogTrace("Writing {Count} float registers to address {Address}",
				floatData.Length, settings.FloatBaseAddr);

			var result = await _chunkHandler.WriteChunkedAsync(
					_transport, settings.FloatBaseAddr, floatData, ChunkSize, ct)
				.ConfigureAwait(false);

			if (result.IsFailed)
				return result;
		}

		_logger.LogTrace("Writing row count {RowCount} to address {Address}",
			rowCount, settings.ControlRegister + OffsetRowCount);

		return await _transport.WriteHoldingAsync(
			settings.ControlRegister + OffsetRowCount,
			new[] { rowCount }, ct).ConfigureAwait(false);
	}
}
