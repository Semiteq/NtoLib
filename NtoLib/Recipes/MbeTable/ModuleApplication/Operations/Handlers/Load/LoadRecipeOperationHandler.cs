using System;
using System.IO;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Csv;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Load;

public sealed class LoadRecipeOperationHandler : IRecipeOperationHandler<LoadRecipeArgs>
{
    private readonly OperationPipeline _pipeline;
    private readonly LoadRecipeOperationDefinition _op;
    private readonly ICsvService _csv;
    private readonly IRecipeService _recipeService;
    private readonly ITimerControl _timer;
    private readonly RecipeViewModel _viewModel;
    private readonly ILogger<LoadRecipeOperationHandler> _logger;

    public LoadRecipeOperationHandler(
        OperationPipeline pipeline,
        LoadRecipeOperationDefinition op,
        ICsvService csv,
        IRecipeService recipeService,
        ITimerControl timer,
        RecipeViewModel viewModel,
        ILogger<LoadRecipeOperationHandler> logger)
    {
        _pipeline = pipeline;
        _op = op;
        _csv = csv;
        _recipeService = recipeService;
        _timer = timer;
        _viewModel = viewModel;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(LoadRecipeArgs args)
    {
        var result = await _pipeline.RunAsync<ValidationSnapshot>(
            _op,
            () => PerformLoadAsync(args.FilePath),
            successMessage: $"Загружен рецепт из {Path.GetFileName(args.FilePath)}");

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _timer.ResetForNewRecipe();
        }

        return result.ToResult();
    }

    private async Task<Result<ValidationSnapshot>> PerformLoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new ApplicationFilePathEmptyError();

        try
        {
            var loadResult = await _csv.ReadCsvAsync(filePath).ConfigureAwait(false);
            if (loadResult.IsFailed)
                return loadResult.ToResult<ValidationSnapshot>();

            var setResult = _recipeService.SetRecipeAndUpdateAttributes(loadResult.Value);
            if (setResult.IsFailed)
                return setResult;

            return setResult.WithReasons(loadResult.Reasons);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during load operation");
            return Result.Fail<ValidationSnapshot>(new ApplicationUnexpectedIoReadError());
        }
    }
}