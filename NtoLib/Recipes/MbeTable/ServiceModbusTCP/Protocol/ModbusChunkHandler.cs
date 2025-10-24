using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

/// <summary>
/// Handles chunked read/write operations for Modbus communication.
/// </summary>
public sealed class ModbusChunkHandler : IModbusChunkHandler
{
    private readonly ILogger<ModbusChunkHandler> _logger;

    public ModbusChunkHandler(ILogger<ModbusChunkHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> WriteChunkedAsync(
        IModbusTransport transport,
        int baseAddress,
        int[] data,
        int chunkSize,
        CancellationToken ct)
    {
        if (transport is null) throw new ArgumentNullException(nameof(transport));
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var offset = 0;
        while (offset < data.Length)
        {
            var size = Math.Min(chunkSize, data.Length - offset);
            var slice = new int[size];
            Array.Copy(data, offset, slice, 0, size);

            _logger.LogTrace("Writing chunk: offset={Offset}, size={Size}", offset, size);
            
            var result = await transport
                .WriteHoldingAsync(baseAddress + offset, slice, ct)
                .ConfigureAwait(false);

            if (result.IsFailed) return result;
            offset += size;
        }
        
        _logger.LogTrace("Successfully wrote {TotalRegisters} registers in chunks", data.Length);
        return Result.Ok();
    }

    public async Task<Result<int[]>> ReadChunkedAsync(
        IModbusTransport transport,
        int baseAddress,
        int totalRegisters,
        int chunkSize,
        CancellationToken ct)
    {
        if (transport is null) throw new ArgumentNullException(nameof(transport));
        if (totalRegisters <= 0) return Result.Ok(Array.Empty<int>());
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var result = new int[totalRegisters];
        var offset = 0;

        while (offset < totalRegisters)
        {
            var size = Math.Min(chunkSize, totalRegisters - offset);
            
            _logger.LogTrace("Reading chunk: offset={Offset}, size={Size}", offset, size);
            
            var readResult = await transport
                .ReadHoldingAsync(baseAddress + offset, size, ct)
                .ConfigureAwait(false);

            if (readResult.IsFailed) 
                return readResult.ToResult<int[]>();

            Array.Copy(readResult.Value, 0, result, offset, size);
            offset += size;
        }

        _logger.LogTrace("Successfully read {TotalRegisters} registers in chunks", totalRegisters);
        return Result.Ok(result);
    }
}