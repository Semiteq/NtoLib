using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

internal sealed class PlcReader : IPlcReader
{
    private const int ChunkSize = 123;
    private const int OffsetRowCount = 1;

    private readonly IModbusTransport _transport;
    private readonly IModbusChunkHandler _chunkHandler;
    private readonly IRuntimeOptionsProvider _optionsProvider;
    private readonly ILogger<PlcReader> _logger;

    public PlcReader(
        IModbusTransport transport,
        IModbusChunkHandler chunkHandler,
        IRuntimeOptionsProvider optionsProvider,
        ILogger<PlcReader> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _chunkHandler = chunkHandler ?? throw new ArgumentNullException(nameof(chunkHandler));
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<int>> ReadRowCountAsync(CancellationToken ct)
    {
        var settings = _optionsProvider.GetCurrent();

        _logger.LogTrace("Reading row count from address {Address}",
            settings.ControlRegister + OffsetRowCount);

        var result = await _transport
            .ReadHoldingAsync(settings.ControlRegister + OffsetRowCount, 1, ct)
            .ConfigureAwait(false);

        if (result.IsFailed)
            return result.ToResult<int>();

        var value = result.Value[0];
        _logger.LogTrace("Read row count: {RowCount}", value);

        return value < 0
            ? Result.Fail(new ModbusTcpInvalidResponseError($"Invalid row count: {value}"))
            : Result.Ok(value);
    }

    public async Task<Result<int[]>> ReadIntAreaAsync(int registers, CancellationToken ct)
    {
        if (registers == 0)
            return Result.Ok(Array.Empty<int>());

        var settings = _optionsProvider.GetCurrent();
        _logger.LogTrace("Reading {Registers} int registers from address {Address}",
            registers, settings.IntBaseAddr);

        return await _chunkHandler.ReadChunkedAsync(
            _transport, settings.IntBaseAddr, registers, ChunkSize, ct)
            .ConfigureAwait(false);
    }

    public async Task<Result<int[]>> ReadFloatAreaAsync(int registers, CancellationToken ct)
    {
        if (registers == 0)
            return Result.Ok(Array.Empty<int>());

        var settings = _optionsProvider.GetCurrent();
        _logger.LogTrace("Reading {Registers} float registers from address {Address}",
            registers, settings.FloatBaseAddr);

        return await _chunkHandler.ReadChunkedAsync(
            _transport, settings.FloatBaseAddr, registers, ChunkSize, ct)
            .ConfigureAwait(false);
    }
}