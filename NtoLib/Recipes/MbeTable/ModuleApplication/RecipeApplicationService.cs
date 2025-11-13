using System;
using System.IO;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IStateProvider _state;
    private readonly ILogger<RecipeApplicationService> _logger;
    private readonly IOperationPipeline _pipeline;
    private readonly ITimerControl _timerControl;

    public RecipeViewModel ViewModel { get; }

    public event Action? RecipeStructureChanged;
    public event Action<int>? StepDataChanged;

    public RecipeApplicationService(
        IRecipeService recipeService,
        IModbusTcpService modbusTcpService,
        ICsvService csvOperations,
        IStateProvider stateProvider,
        RecipeViewModel viewModel,
        ILogger<RecipeApplicationService> logger,
        IOperationPipeline pipeline,
        ITimerControl timerControl)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _modbusTcpService = modbusTcpService ?? throw new ArgumentNullException(nameof(modbusTcpService));
        _csvOperations = csvOperations ?? throw new ArgumentNullException(nameof(csvOperations));
        _state = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _timerControl = timerControl ?? throw new ArgumentNullException(nameof(timerControl));
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _state.SetStepCount(_recipeService.StepCount);
        _state.SetRecipeConsistent(false);
    }

    public Recipe GetCurrentRecipe() => _recipeService.CurrentRecipe;

    public int GetRowCount() => _recipeService.StepCount;

    public Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        return ExecuteCellUpdateAsync(rowIndex, columnKey, value);
    }

    public Result AddStep(int index)
    {
        return ExecuteAddStep(index);
    }

    public Result RemoveStep(int index)
    {
        return ExecuteRemoveStep(index);
    }

    public Task<Result> LoadRecipeAsync(string filePath)
    {
        return ExecuteLoadAsync(filePath);
    }

    public Task<Result> SaveRecipeAsync(string filePath)
    {
        return ExecuteSaveAsync(filePath);
    }

    public Task<Result> SendRecipeAsync()
    {
        return ExecuteSendAsync();
    }

    public Task<Result> ReceiveRecipeAsync()
    {
        return ExecuteReceiveAsync();
    }

    private async Task<Result> ExecuteCellUpdateAsync(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        var pipelineResult = await _pipeline.RunAsync<ValidationSnapshot>(
            OperationId.EditCell,
            OperationKind.None,
            () => PerformCellUpdate(rowIndex, columnKey, value),
            successMessage: null,
            affectsRecipe: true).ConfigureAwait(false);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(false);
            NotifyCellChanged(rowIndex);
        }

        return pipelineResult.ToResult();
    }

    private Task<Result<ValidationSnapshot>> PerformCellUpdate(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        var validation = ValidateRowIndex(rowIndex, _recipeService.StepCount);
        if (validation.IsFailed)
        {
            _logger.LogWarning("SetCellValueAsync validation failed: rowIndex={RowIndex}", rowIndex);
            return Task.FromResult(validation.ToResult<ValidationSnapshot>());
        }

        var result = ApplyPropertyUpdate(rowIndex, columnKey, value);
        return Task.FromResult(result);
    }

    private Result ExecuteAddStep(int index)
    {
        var pipelineResult = _pipeline.Run<ValidationSnapshot>(
            OperationId.AddStep,
            OperationKind.None,
            () => PerformAddStep(index),
            successMessage: $"Добавлена строка №{index + 1}",
            affectsRecipe: true);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(false);
            NotifyStructureChanged();
        }

        return pipelineResult.ToResult();
    }

    private Result<ValidationSnapshot> PerformAddStep(int index)
    {
        return _recipeService.AddStep(index);
    }

    private Result ExecuteRemoveStep(int index)
    {
        var pipelineResult = _pipeline.Run<ValidationSnapshot>(
            OperationId.RemoveStep,
            OperationKind.None,
            () => PerformRemoveStep(index),
            successMessage: $"Удалена строка №{index + 1}",
            affectsRecipe: true);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(false);
            NotifyStructureChanged();
        }

        return pipelineResult.ToResult();
    }

    private Result<ValidationSnapshot> PerformRemoveStep(int index)
    {
        return _recipeService.RemoveStep(index);
    }

    private async Task<Result> ExecuteLoadAsync(string filePath)
    {
        var pipelineResult = await _pipeline.RunAsync<ValidationSnapshot>(
            OperationId.Load,
            OperationKind.Loading,
            () => PerformLoadAsync(filePath),
            successMessage: $"Загружен рецепт из {Path.GetFileName(filePath)}",
            affectsRecipe: true).ConfigureAwait(false);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(false);
            NotifyStructureChanged();
        }

        return pipelineResult.ToResult();
    }

    private async Task<Result<ValidationSnapshot>> PerformLoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new ApplicationFilePathEmptyError();

        try
        {
            var loadResult = await _csvOperations.ReadCsvAsync(filePath).ConfigureAwait(false);
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

    private async Task<Result> ExecuteSaveAsync(string filePath)
    {
        var pipelineResult = await _pipeline.RunAsync(
            OperationId.Save,
            OperationKind.Saving,
            () => PerformSaveAsync(filePath),
            successMessage: $"Рецепт сохранен в {Path.GetFileName(filePath)}",
            affectsRecipe: false).ConfigureAwait(false);

        return pipelineResult;
    }

    private async Task<Result> PerformSaveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new ApplicationFilePathEmptyError();

        try
        {
            var currentRecipe = _recipeService.CurrentRecipe;
            return await _csvOperations.WriteCsvAsync(currentRecipe, filePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during save operation");
            return Result.Fail(new ApplicationUnexpectedIoWriteError());
        }
    }

    private async Task<Result> ExecuteSendAsync()
    {
        var pipelineResult = await _pipeline.RunAsync(
            OperationId.Send,
            OperationKind.Transferring,
            PerformSendAsync,
            successMessage: "Рецепт успешно отправлен в контроллер",
            affectsRecipe: false).ConfigureAwait(false);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(true);
        }

        return pipelineResult;
    }

    private async Task<Result> PerformSendAsync()
    {
        if (!_state.GetSnapshot().EnaSendOk)
        {
            _logger.LogWarning("Send operation blocked by PLC logic (EnaSendOk is false)");
            return Result.Fail(new ApplicationSendBlockedByPlcError());
        }

        try
        {
            var currentRecipe = _recipeService.CurrentRecipe;
            return await _modbusTcpService.SendRecipeAsync(currentRecipe).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during send operation");
            return Result.Fail(new ApplicationUnexpectedIoWriteError());
        }
    }

    private async Task<Result> ExecuteReceiveAsync()
    {
        var pipelineResult = await _pipeline.RunAsync<ValidationSnapshot>(
            OperationId.Receive,
            OperationKind.Transferring,
            PerformReceiveAsync,
            successMessage: "Рецепт успешно прочитан из контроллера",
            affectsRecipe: true).ConfigureAwait(false);

        if (pipelineResult.IsSuccess)
        {
            _state.SetRecipeConsistent(false);
            NotifyStructureChanged();
        }

        return pipelineResult.ToResult();
    }

    private async Task<Result<ValidationSnapshot>> PerformReceiveAsync()
    {
        try
        {
            var receiveResult = await _modbusTcpService.ReceiveRecipeAsync().ConfigureAwait(false);
            if (receiveResult.IsFailed || receiveResult.Value == null)
                return receiveResult.ToResult<ValidationSnapshot>();

            var setResult = _recipeService.SetRecipeAndUpdateAttributes(receiveResult.Value);
            if (setResult.IsFailed)
                return setResult;

            return setResult.WithReasons(receiveResult.Reasons);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error during receive operation");
            return Result.Fail<ValidationSnapshot>(new ApplicationUnexpectedIoReadError());
        }
    }

    private static Result ValidateRowIndex(int rowIndex, int rowCount)
    {
        if (rowIndex < 0 || rowIndex > rowCount)
            return new ApplicationIndexOutOfRangeError(rowIndex, rowCount);

        return Result.Ok();
    }

    private Result<ValidationSnapshot> ApplyPropertyUpdate(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        if (columnKey == MandatoryColumns.Action && value is short actionId)
            return _recipeService.ReplaceStepAction(rowIndex, actionId);

        return _recipeService.UpdateStepProperty(rowIndex, columnKey, value);
    }

    private void NotifyCellChanged(int rowIndex)
    {
        _timerControl.ResetForNewRecipe();
        ViewModel.OnTimeRecalculated(rowIndex);
        RaiseStepDataChanged(rowIndex);
    }

    private void NotifyStructureChanged()
    {
        _state.SetStepCount(_recipeService.StepCount);
        ViewModel.OnRecipeStructureChanged();
        _timerControl.ResetForNewRecipe();
        RaiseRecipeStructureChanged();
    }

    private void RaiseRecipeStructureChanged()
    {
        try
        {
            RecipeStructureChanged?.Invoke();
        }
        catch
        {
        }
    }

    private void RaiseStepDataChanged(int rowIndex)
    {
        try
        {
            StepDataChanged?.Invoke(rowIndex);
        }
        catch
        {
        }
    }
}