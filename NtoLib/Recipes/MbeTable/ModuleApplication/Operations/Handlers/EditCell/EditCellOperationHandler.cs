using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.EditCell;

public sealed class EditCellOperationHandler : IRecipeOperationHandler<EditCellArgs>
{
    private readonly OperationPipeline _pipeline;
    private readonly EditCellOperationDefinition _op;
    private readonly IRecipeFacade _recipeService;
    private readonly ITimerService _timer;
    private readonly RecipeViewModel _viewModel;
    private readonly ILogger<EditCellOperationHandler> _logger;

    public EditCellOperationHandler(
        OperationPipeline pipeline,
        EditCellOperationDefinition op,
        IRecipeFacade recipeService,
        ITimerService timer,
        RecipeViewModel viewModel,
        ILogger<EditCellOperationHandler> logger)
    {
        _pipeline = pipeline;
        _op = op;
        _recipeService = recipeService;
        _timer = timer;
        _viewModel = viewModel;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(EditCellArgs args)
    {
        var result = await _pipeline.RunAsync(
            _op,
            () => PerformCellUpdateAsync(args.RowIndex, args.ColumnKey, args.Value),
            successMessage: null);

        if (result.IsSuccess)
        {
            _timer.Reset();
            _viewModel.OnTimeRecalculated(args.RowIndex);
        }

        return result.ToResult();
    }

    private Task<Result<RecipeAnalysisSnapshot>> PerformCellUpdateAsync(int rowIndex, ColumnIdentifier columnKey,
        object value)
    {
        var validation = ValidateExistingRowIndex(rowIndex, _recipeService.CurrentSnapshot.StepCount);
        if (validation.IsFailed)
        {
            _logger.LogWarning("EditCell validation failed: rowIndex={RowIndex}", rowIndex);
            return Task.FromResult(validation.ToResult<RecipeAnalysisSnapshot>());
        }

        var applyResult = ApplyPropertyUpdate(rowIndex, columnKey, value);
        return Task.FromResult(applyResult);
    }

    private static Result ValidateExistingRowIndex(int rowIndex, int rowCount)
    {
        if (rowIndex < 0 || rowIndex >= rowCount)
            return new ApplicationInvalidRowIndexError(rowIndex);

        return Result.Ok();
    }

    private Result<RecipeAnalysisSnapshot> ApplyPropertyUpdate(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        if (columnKey == MandatoryColumns.Action && value is short actionId)
            return _recipeService.ReplaceAction(rowIndex, actionId);

        return _recipeService.UpdateProperty(rowIndex, columnKey, value);
    }
}