using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Warnings;

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
        _logger.LogDebug("Sending recipe to PLC");
        
        try
        {
            var settings = _runtimeOptionsProvider.GetCurrent();
            var verifyDelayMs = settings.VerifyDelayMs;

            using var _ = MetricsStopwatch.Start("SendRecipe", _logger);

            _logger.LogDebug("Checking PLC capacity for {StepCount} steps.", recipe.Steps.Count);
            var capacityCheck = _capacity.TryCheckCapacity(recipe);
            if (capacityCheck.IsFailed)
            {
                _logger.LogError("PLC capacity check failed: {Errors}", capacityCheck.Errors);
                return capacityCheck;
            }

            var serializeResult = _serializer.ToRegisters(recipe.Steps);
            if (serializeResult.IsFailed) return serializeResult.ToResult();
            var (intArr, floatArr) = serializeResult.Value;

            var writeResult = await _writer
                .WriteAllAreasAsync(intArr, floatArr, recipe.Steps.Count, ct)
                .ConfigureAwait(false);

            if (writeResult.IsFailed)
            {
                _logger.LogError("Writing to PLC failed: {Errors}", writeResult.Errors);
                return writeResult;
            }

            if (verifyDelayMs > 0)
            {
                _logger.LogDebug("Waiting for {Delay} ms before verification.", verifyDelayMs);
                await Task.Delay(verifyDelayMs, ct).ConfigureAwait(false);
            }

            return Result.Ok().WithSuccess("Recipe successfully written to PLC");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Send operation was cancelled."); 
            return Result.Fail(new BilingualError("Operation cancelled", "Операция отменена"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during recipe serialization or sending.");
            return Result.Fail(new BilingualError(
                "An unexpected error occurred during send operation",
                "Непредвиденная ошибка при отправке рецепта").CausedBy(ex));
        }
        finally
        {
            if (_disconnectStrategy.ShouldDisconnect("Send"))
            {
                _logger.LogDebug("Disconnecting after Send operation as per strategy.");
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
                return Result.Ok((Array.Empty<int>(), Array.Empty<int>(), 0))
                    .WithReason(new ModbusTcpZeroRowsWarning());
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
            return Result.Fail(new BilingualError("Operation cancelled", "Операция отменена"))
                .ToResult<(int[], int[], int)>();
        }
    }
}