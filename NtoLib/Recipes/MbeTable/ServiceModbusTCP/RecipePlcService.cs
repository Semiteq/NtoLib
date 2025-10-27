using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP;

public sealed class RecipePlcService : IRecipePlcService
{
    private readonly IPlcWriter _writer;
    private readonly IPlcReader _reader;
    private readonly IModbusTransport _transport;
    private readonly PlcRecipeSerializer _serializer;
    private readonly PlcCapacityCalculator _capacity;
    private readonly RecipeColumnLayout _layout;
    private readonly IRuntimeOptionsProvider _runtimeOptionsProvider;
    private readonly IDisconnectStrategy _disconnectStrategy;
    private readonly ILogger<RecipePlcService> _logger;

    public RecipePlcService(
        IPlcWriter writer,
        IPlcReader reader,
        IModbusTransport transport,
        PlcRecipeSerializer serializer,
        PlcCapacityCalculator capacity,
        RecipeColumnLayout layout,
        IRuntimeOptionsProvider runtimeOptionsProvider,
        IDisconnectStrategy disconnectStrategy,
        ILogger<RecipePlcService> logger)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _runtimeOptionsProvider = runtimeOptionsProvider ?? throw new ArgumentNullException(nameof(runtimeOptionsProvider));
        _disconnectStrategy = disconnectStrategy ?? throw new ArgumentNullException(nameof(disconnectStrategy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendAsync(Recipe recipe, CancellationToken ct = default)
    {
        if (recipe is null)
            return Result.Fail(new Error("Recipe is null").WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));

        try
        {
            var settings = _runtimeOptionsProvider.GetCurrent();
            var verifyDelayMs = settings.VerifyDelayMs;

            using var _ = MetricsStopwatch.Start("SendRecipe", _logger);

            var capacityCheck = _capacity.TryCheckCapacity(recipe);
            if (capacityCheck.IsFailed)
                return capacityCheck;

            var (intArr, floatArr) = _serializer.ToRegisters(recipe.Steps);

            var writeResult = await _writer
                .WriteAllAreasAsync(intArr, floatArr, recipe.Steps.Count, ct)
                .ConfigureAwait(false);

            if (writeResult.IsFailed)
                return writeResult;

            if (verifyDelayMs > 0)
                await Task.Delay(verifyDelayMs, ct).ConfigureAwait(false);

            return Result.Ok().WithSuccess("Recipe successfully written to PLC");
        }
        catch (OperationCanceledException)
        {
            return Result.Fail(new Error("Operation cancelled").WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));
        }
        finally
        {
            if (_disconnectStrategy.ShouldDisconnect("Send"))
            {
                _transport.Disconnect();
            }
        }
    }

    public async Task<Result<(int[] IntData, int[] FloatData, int RowCount)>> ReceiveAsync(
        CancellationToken ct = default)
    {
        try
        {
            using var _ = MetricsStopwatch.Start("ReceiveRecipe", _logger);

            var rowResult = await _reader.ReadRowCountAsync(ct).ConfigureAwait(false);
            if (rowResult.IsFailed)
                return rowResult.ToResult<(int[], int[], int)>();

            var rows = rowResult.Value;
            if (rows == 0)
            {
                _logger.LogDebug("No rows in PLC. Nothing to read");
                return Result.Ok((Array.Empty<int>(), Array.Empty<int>(), 0));
            }

            _logger.LogDebug("Received {RowCount} rows from PLC", rows);

            var validationResult = _capacity.ValidateReadCapacity(rows);
            if (validationResult.IsFailed)
                return validationResult.ToResult<(int[], int[], int)>();

            var intSize = _layout.IntColumnCount * rows;
            var floatSize = _layout.FloatColumnCount * 2 * rows;

            var intResult = await _reader.ReadIntAreaAsync(intSize, ct).ConfigureAwait(false);
            if (intResult.IsFailed)
                return intResult.ToResult<(int[], int[], int)>();

            var floatResult = await _reader.ReadFloatAreaAsync(floatSize, ct).ConfigureAwait(false);
            if (floatResult.IsFailed)
                return floatResult.ToResult<(int[], int[], int)>();

            return Result.Ok((intResult.Value, floatResult.Value, rows));
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Operation cancelled")
                .ToResult<(int[], int[], int)>();
        }
    }
}