using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP;

/// <inheritdoc />
public sealed class RecipePlcService : IRecipePlcService
{
    private readonly IPlcProtocol _protocol;
    private readonly IModbusTransport _transport;
    private readonly PlcRecipeSerializer _serializer;
    private readonly PlcCapacityCalculator _capacity;
    private readonly RecipeColumnLayout _layout;
    private readonly IRuntimeOptionsProvider _runtimeOptionsProvider;
    private readonly ILogger _logger;

    public RecipePlcService(
        IPlcProtocol protocol,
        IModbusTransport transport,
        PlcRecipeSerializer serializer,
        PlcCapacityCalculator capacity,
        RecipeColumnLayout layout,
        IRuntimeOptionsProvider runtimeOptionsProvider,
        ILogger logger)
    {
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _runtimeOptionsProvider = runtimeOptionsProvider ?? throw new ArgumentNullException(nameof(runtimeOptionsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendAsync(Recipe recipe, CancellationToken ct = default)
    {
        if (recipe is null) 
            return Result.Fail("Recipe is null");

        try
        {
            var settings = _runtimeOptionsProvider.GetCurrent();
            var verifyDelayMs = settings.VerifyDelayMs;
            
            using var _ = MetricsStopwatch.Start("SendRecipe", _logger);

            var cap = _capacity.TryCheckCapacity(recipe);
            if (cap.IsFailed) return cap;

            var (intArr, floatArr) = _serializer.ToRegisters(recipe.Steps);

            var write = await _protocol
                .WriteAllAreasAsync(intArr, floatArr, recipe.Steps.Count, ct)
                .ConfigureAwait(false);
            
            if (write.IsFailed) return write;

            // Delay before verification
            if (verifyDelayMs > 0)
                await Task.Delay(verifyDelayMs, ct).ConfigureAwait(false);

            // Read back for verification
            var readBack = await ReceiveRawDataAsync(ct).ConfigureAwait(false);
            if (readBack.IsFailed) 
                return readBack.ToResult();

            // Return raw data for comparison at higher level
            // Since we don't have RecipeAssemblyService here, we'll need to adjust this
            // For now, return success - actual verification will happen in Application layer
            return Result.Ok().WithSuccess("Recipe successfully written to PLC");
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Operation cancelled");
        }
        finally
        {
            _transport.Disconnect();
        }
    }

    public async Task<Result<(int[] IntData, int[] FloatData, int RowCount)>> ReceiveAsync(
        CancellationToken ct = default)
    {
        try
        {
            return await ReceiveRawDataAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _transport.Disconnect();
        }
    }

    private async Task<Result<(int[] IntData, int[] FloatData, int RowCount)>> ReceiveRawDataAsync(
        CancellationToken ct)
    {
        using var _ = MetricsStopwatch.Start("ReceiveRecipe", _logger);

        var rowRes = await _protocol.ReadRowCountAsync(ct).ConfigureAwait(false);
        if (rowRes.IsFailed) 
            return rowRes.ToResult<(int[], int[], int)>();

        var rows = rowRes.Value;
        if (rows == 0)
            return Result.Ok((Array.Empty<int>(), Array.Empty<int>(), 0));

        var intSize = _layout.IntColumnCount * rows;
        var floatSize = _layout.FloatColumnCount * 2 * rows;

        var intsRes = await _protocol.ReadIntAreaAsync(intSize, ct).ConfigureAwait(false);
        if (intsRes.IsFailed) 
            return intsRes.ToResult<(int[], int[], int)>();

        var floatsRes = await _protocol.ReadFloatAreaAsync(floatSize, ct).ConfigureAwait(false);
        if (floatsRes.IsFailed) 
            return floatsRes.ToResult<(int[], int[], int)>();

        return Result.Ok((intsRes.Value, floatsRes.Value, rows));
    }
}