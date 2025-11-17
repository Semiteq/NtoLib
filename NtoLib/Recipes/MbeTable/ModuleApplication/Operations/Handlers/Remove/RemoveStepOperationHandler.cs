using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Remove;

public sealed class RemoveStepOperationHandler : IRecipeOperationHandler<RemoveStepArgs>
{
    private readonly OperationPipeline _pipeline;
    private readonly RemoveStepOperationDefinition _op;
    private readonly ITimerService _timer;
    private readonly RecipeViewModel _viewModel;
    private readonly IRecipeFacade _recipeService;

    public RemoveStepOperationHandler(
        OperationPipeline pipeline,
        RemoveStepOperationDefinition op,
        ITimerService timer,
        RecipeViewModel viewModel,
        IRecipeFacade recipeService)
    {
        _pipeline = pipeline;
        _op = op;
        _timer = timer;
        _viewModel = viewModel;
        _recipeService = recipeService;
    }

    public async Task<Result> ExecuteAsync(RemoveStepArgs args)
    {
        var result = await _pipeline.RunAsync(
            _op,
            () => Task.FromResult(_recipeService.RemoveStep(args.Index)),
            successMessage: $"Удалена строка №{args.Index + 1}");

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _timer.Reset();
        }

        return result.ToResult();
    }
}