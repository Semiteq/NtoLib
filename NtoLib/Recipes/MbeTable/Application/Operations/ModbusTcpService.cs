

using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.ModbusTCP;
using NtoLib.Recipes.MbeTable.ModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.RecipeAssemblyService;

namespace NtoLib.Recipes.MbeTable.Application.Operations;

/// <summary>
/// Implements async recipe operations (file IO and PLC exchange) without any direct UI
/// or logging dependencies. Uses <see cref="FluentResults"/> for error propagation.
/// </summary>
public sealed class ModbusTcpService : IModbusTcpService
{
    private readonly IRecipePlcService _plcService;
    private readonly IRecipeAssemblyService _assemblyService;
    private readonly RecipeComparator _comparator;

    public ModbusTcpService(
        IRecipePlcService plcService,
        IRecipeAssemblyService assemblyService,
        RecipeComparator comparator) 
    {
        _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
        _assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
        _comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
    }

    public async Task<Result> SendRecipeAsync(Recipe recipe)
    {
        if (recipe is null)
            return Result.Fail("Recipe is null.");

        // Send recipe to PLC
        var sendResult = await _plcService.SendAsync(recipe, CancellationToken.None);
        if (sendResult.IsFailed)
            return sendResult;

        // Read back for verification
        var readResult = await _plcService.ReceiveAsync(CancellationToken.None);
        if (readResult.IsFailed)
            return readResult.ToResult();

        // Assemble recipe from raw data
        var (intData, floatData, rowCount) = readResult.Value;
        var assembleResult = _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
        if (assembleResult.IsFailed)
            return Result.Fail("Failed to verify: could not assemble recipe from PLC data")
                .WithErrors(assembleResult.Errors);

        // Compare with original
        var compareResult = _comparator.Compare(recipe, assembleResult.Value);
        if (compareResult.IsFailed)
            return Result.Fail("Verification failed: PLC data does not match sent recipe")
                .WithErrors(compareResult.Errors);

        return Result.Ok().WithSuccess("Recipe successfully sent and verified");
    }

    public async Task<Result<Recipe>> ReceiveRecipeAsync()
    {
        var readResult = await _plcService.ReceiveAsync(CancellationToken.None);
        if (readResult.IsFailed)
            return readResult.ToResult<Recipe>();

        var (intData, floatData, rowCount) = readResult.Value;
        return _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
    }
}