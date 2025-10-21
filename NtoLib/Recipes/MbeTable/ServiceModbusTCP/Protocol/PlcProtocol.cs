using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

internal sealed class PlcProtocol : IPlcProtocol
{
    private const int Chunk = 123;
    private const int OffsetRowCount = 1;

    private readonly IModbusTransport _transport;
    private readonly IRuntimeOptionsProvider _optionsProvider;
    private readonly ILogger _logger;

    public PlcProtocol(
        IModbusTransport transport,
        IRuntimeOptionsProvider optionsProvider,
        ILogger logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> WriteAllAreasAsync(
        int[] intData, int[] floatData, int rowCount, CancellationToken ct)
    {
        using var _ = MetricsStopwatch.Start("WriteAllAreas", _logger);
        
        var settings = _optionsProvider.GetCurrent();

        if (intData.Length > 0)
        {
            var r = await WriteChunkedAsync(settings.IntBaseAddr, intData, ct)
                .ConfigureAwait(false);
            if (r.IsFailed) return r;
        }

        if (floatData.Length > 0)
        {
            var r = await WriteChunkedAsync(settings.FloatBaseAddr, floatData, ct)
                .ConfigureAwait(false);
            if (r.IsFailed) return r;
        }

        return await _transport.WriteHoldingAsync(
            settings.ControlRegister + OffsetRowCount,
            new[] { rowCount }, ct).ConfigureAwait(false);
    }

    public async Task<Result<int>> ReadRowCountAsync(CancellationToken ct)
    {
        var settings = _optionsProvider.GetCurrent();
        
        var result = await _transport
            .ReadHoldingAsync(settings.ControlRegister + OffsetRowCount, 1, ct)
            .ConfigureAwait(false);

        if (result.IsFailed)
            return result.ToResult<int>();

        var val = result.Value[0];
        return val < 0
            ? Result.Fail(new Error("Invalid row count")
                .WithMetadata(nameof(Codes), Codes.PlcInvalidRowCount))
            : Result.Ok(val);
    }

    public Task<Result<int[]>> ReadIntAreaAsync(int regs, CancellationToken ct)
    {
        if (regs == 0)
            return Task.FromResult(Result.Ok(Array.Empty<int>()));
        
        var settings = _optionsProvider.GetCurrent();
        return _transport.ReadHoldingAsync(settings.IntBaseAddr, regs, ct);
    }

    public Task<Result<int[]>> ReadFloatAreaAsync(int regs, CancellationToken ct)
    {
        if (regs == 0)
            return Task.FromResult(Result.Ok(Array.Empty<int>()));
        
        var settings = _optionsProvider.GetCurrent();
        return _transport.ReadHoldingAsync(settings.FloatBaseAddr, regs, ct);
    }

    private async Task<Result> WriteChunkedAsync(int baseAddr, int[] data, CancellationToken ct)
    {
        var offset = 0;
        while (offset < data.Length)
        {
            var size = Math.Min(Chunk, data.Length - offset);
            var slice = new int[size];
            Array.Copy(data, offset, slice, 0, size);

            var res = await _transport
                .WriteHoldingAsync(baseAddr + offset, slice, ct)
                .ConfigureAwait(false);

            if (res.IsFailed) return res;
            offset += size;
        }
        return Result.Ok();
    }
}