#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Services;

public sealed class RecipePlcSender : IRecipePlcSender
{
    private readonly IModbusTransport _modbusTransport;
    private readonly IPlcProtocol _plcProtocol;
    private readonly IPlcRecipeSerializer _plcRecipeSerializer;
    private readonly IRecipeComparator _recipeComparator;
    private readonly PlcCapacityCalculator _plcCapacityCalculator;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;
    private readonly TableColumns _tableColumns;

    public RecipePlcSender(
        IModbusTransport modbusTransport,
        IPlcProtocol plcProtocol,
        IPlcRecipeSerializer plcRecipeSerializer,
        IRecipeComparator recipeComparator,
        PlcCapacityCalculator plcCapacityCalculator,
        ICommunicationSettingsProvider communicationSettingsProvider,
        ILogger debugLogger,
        TableColumns tableColumns)
    {
        _modbusTransport = modbusTransport ?? throw new ArgumentNullException(nameof(modbusTransport));
        _plcProtocol = plcProtocol ?? throw new ArgumentNullException(nameof(plcProtocol));
        _plcRecipeSerializer = plcRecipeSerializer ?? throw new ArgumentNullException(nameof(plcRecipeSerializer));
        _recipeComparator = recipeComparator ?? throw new ArgumentNullException(nameof(recipeComparator));
        _plcCapacityCalculator = plcCapacityCalculator ?? throw new ArgumentNullException(nameof(plcCapacityCalculator));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public async Task<Result> SendAndVerifyRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        try
        {
            var capacityResult = _plcCapacityCalculator.TryCheckCapacity(recipe, Settings);
            if (capacityResult.IsFailed)
                return capacityResult;

            var connectResult = _modbusTransport.Connect();
            if (connectResult.IsFailed)
                return connectResult;

            var (ints, floats) = _plcRecipeSerializer.ToRegisters(recipe.Steps);

            var writeRes = _plcProtocol.WriteAllAreas(ints, floats, recipe.Steps.Count);
            if (writeRes.IsFailed)
                return writeRes;

            await Task.Delay(Math.Max(0, Settings.VerifyDelayMs), cancellationToken);

            var readBackRecipeResult = await ReciveRecipeInternalAsync(cancellationToken);
            if (readBackRecipeResult.IsFailed)
                return readBackRecipeResult.ToResult();
            
            var back = readBackRecipeResult.Value;

            var compareResult = _recipeComparator.Compare(recipe, back);
            if (compareResult.IsFailed)
            {
                var errors = string.Join(", ", compareResult.Errors.Select(e => e.Message));
                _debugLogger.Log("Verification failed: PLC data does not match uploaded recipe.");
                return Result.Fail($"Verification failed: PLC data differs from the uploaded recipe. Error: {errors}");
            }

            return Result.Ok().WithSuccess("Recipe successfully written and verified.");
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Operation cancelled.");
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Unexpected error during recipe sending: {ex}");
            return Result.Fail("An unexpected error occurred while writing the recipe.");
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }

    public async Task<Result<Recipe>> ReciveRecipeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectResult = _modbusTransport.Connect();
            if (connectResult.IsFailed)
                return Result.Fail<Recipe>(connectResult.Errors);

            return await ReciveRecipeInternalAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Unexpected error on download: {ex}");
            return Result.Fail<Recipe>("An unexpected error occurred while reading the recipe.");
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }

    private async Task<Result<Recipe>> ReciveRecipeInternalAsync(CancellationToken cancellationToken)
    {
        var rowCountResult = _plcProtocol.ReadRowCount();
        if (rowCountResult.IsFailed)
            return rowCountResult.ToResult<Recipe>();

        var rowCount = rowCountResult.Value;
        if (rowCount == 0)
            return Result.Ok(new Recipe(ImmutableList<Step>.Empty));

        var intCols = GetColumnCountForArea("Int");
        var floatCols = GetColumnCountForArea("Float");

        var intQty = rowCount * intCols;
        var floatQty = rowCount * floatCols * 2;

        var intReadRes = _plcProtocol.ReadIntArea(intQty);
        if (intReadRes.IsFailed)
            return intReadRes.ToResult<Recipe>();

        var floatReadRes = _plcProtocol.ReadFloatArea(floatQty);
        if (floatReadRes.IsFailed)
            return floatReadRes.ToResult<Recipe>();

        var steps = _plcRecipeSerializer.FromRegisters(intReadRes.Value, floatReadRes.Value, rowCount);
        var recipe = new Recipe(steps.ToImmutableList());
        return Result.Ok(recipe);
    }
    
    private int GetColumnCountForArea(string area)
    {
        var maxIndex = _tableColumns.GetColumns()
            .Where(c => c.PlcMapping?.Area.Equals(area, StringComparison.OrdinalIgnoreCase) ?? false)
            .Max(c => c.PlcMapping?.Index) ?? -1;
        
        return maxIndex + 1;
    }
}