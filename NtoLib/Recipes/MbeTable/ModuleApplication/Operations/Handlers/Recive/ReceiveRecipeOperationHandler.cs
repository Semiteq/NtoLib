using System;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Recive;

public sealed class ReceiveRecipeOperationHandler : IRecipeOperationHandler<ReceiveRecipeArgs>
{
    private readonly OperationPipeline _pipeline;
    private readonly ReceiveRecipeOperationDefinition _op;
    private readonly IModbusTcpService _modbus;
    private readonly IRecipeFacade _recipeService;
    private readonly ITimerService _timer;
    private readonly RecipeViewModel _viewModel;
    private readonly ILogger<ReceiveRecipeOperationHandler> _logger;

    public ReceiveRecipeOperationHandler(
        OperationPipeline pipeline,
        ReceiveRecipeOperationDefinition op,
        IModbusTcpService modbus,
        IRecipeFacade recipeService,
        ITimerService timer,
        RecipeViewModel viewModel,
        ILogger<ReceiveRecipeOperationHandler> logger)
    {
        _pipeline = pipeline;
        _op = op;
        _modbus = modbus;
        _recipeService = recipeService;
        _timer = timer;
        _viewModel = viewModel;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(ReceiveRecipeArgs args)
    {
        var result = await _pipeline.RunAsync(
            _op,
            PerformReceiveAsync,
            successMessage: "Рецепт успешно прочитан из контроллера");

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _timer.Reset();
        }

        return result.ToResult();
    }

    private async Task<Result<RecipeAnalysisSnapshot>> PerformReceiveAsync()
    {
        try
        {
            var receiveResult = await _modbus.ReceiveRecipeAsync().ConfigureAwait(false);
            if (receiveResult.IsFailed || receiveResult.Value == null)
                return receiveResult.ToResult<RecipeAnalysisSnapshot>();

            var setResult = _recipeService.LoadRecipe(receiveResult.Value);
            if (setResult.IsFailed)
                return setResult;

            return setResult.WithReasons(receiveResult.Reasons);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during receive operation");
            return Result.Fail<RecipeAnalysisSnapshot>(new ApplicationUnexpectedIoReadError());
        }
    }
}