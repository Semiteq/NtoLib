using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

/// <summary>
/// Implements async recipe operations (file IO and PLC exchange) without any direct UI
/// or logging dependencies. Uses FluentResults for error propagation.
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
        if (recipe is null) return Result.Fail("Recipe is null.");

        var sendResult = await _plcService.SendAsync(recipe, CancellationToken.None);
        if (sendResult.IsFailed) return sendResult;

        var receiveResult = await _plcService.ReceiveAsync(CancellationToken.None);
        if (receiveResult.IsFailed) return receiveResult.ToResult();

        var (intData, floatData, rowCount) = receiveResult.Value;
        
        if (rowCount == 0) return Result.Ok().WithSuccess(
            new Success("PLC returned zero rows; verification skipped")
            .WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));
        
        var assembleResult = _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
        if (assembleResult.IsFailed) return assembleResult.ToResult();
        
        var compareResult = _comparator.Compare(recipe, assembleResult.Value);
        return compareResult;
    }

    public async Task<Result<Recipe>> ReceiveRecipeAsync()
    {
        var readResult = await _plcService.ReceiveAsync(CancellationToken.None);
        if (readResult.IsFailed) return readResult.ToResult();

        var (intData, floatData, rowCount) = readResult.Value;
        
        if (rowCount == 0) return Result.Ok().
            WithSuccess(new Success("PLC returned zero rows; verification skipped").
                WithMetadata(nameof(Codes), Codes.CoreInvalidOperation));
        
        var assembleResult = _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
        return assembleResult;
    }
}