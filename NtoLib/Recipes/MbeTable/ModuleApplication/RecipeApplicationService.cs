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
    private const string OpLoad = "загрузка рецепта";
    private const string OpSave = "сохранение рецепта";
    private const string OpSend = "отправка рецепта";
    private const string OpReceive = "чтение рецепта";

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

        // Initialize consistency at startup.
        _state.SetRecipeConsistent(false);

        _state.SetStepCount(_recipeService.StepCount);
    }

    public Recipe GetCurrentRecipe() => _recipeService.CurrentRecipe;

    public int GetRowCount() => _recipeService.StepCount;

    public Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        return _pipeline.RunAsync(
            OperationId.EditCell,
            OperationKind.None,
            async () =>
            {
                var validation = ValidateRowIndex(rowIndex, _recipeService.StepCount);
                if (validation.IsFailed)
                {
                    _logger.LogWarning("SetCellValueAsync validation failed: rowIndex={RowIndex}", rowIndex);
                    return validation;
                }

                var result = ApplyPropertyUpdate(rowIndex, columnKey, value);
                if (result.IsSuccess)
                {
                    // Any mutation resets consistency.
                    _state.SetRecipeConsistent(false);
                    UpdateAfterCellChange(rowIndex);
                }

                return await Task.FromResult(result).ConfigureAwait(false);
            },
            successMessage: null,
            affectsRecipe: true);
    }

    public Result AddStep(int index)
    {
        return _pipeline.Run(
            OperationId.AddStep,
            OperationKind.None,
            () =>
            {
                var result = _recipeService.AddStep(index);

                if (result.IsSuccess)
                {
                    _state.SetRecipeConsistent(false);
                    UpdateAfterStructureChange();
                }

                return result;
            },
            successMessage: $"Добавлена строка №{index + 1}",
            affectsRecipe: true);
    }

    public Result RemoveStep(int index)
    {
        return _pipeline.Run(
            OperationId.RemoveStep,
            OperationKind.None,
            () =>
            {
                var result = _recipeService.RemoveStep(index);

                if (result.IsSuccess)
                {
                    _state.SetRecipeConsistent(false);
                    UpdateAfterStructureChange();
                }

                return result;
            },
            successMessage: $"Удалена строка №{index + 1}",
            affectsRecipe: true);
    }

    public Task<Result> LoadRecipeAsync(string filePath)
    {
        return _pipeline.RunAsync(
            OperationId.Load,
            OperationKind.Loading,
            async () =>
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return new ApplicationFilePathEmptyError();

                try
                {
                    var loadResult = await _csvOperations.ReadCsvAsync(filePath).ConfigureAwait(false);
                    if (loadResult.IsFailed)
                        return loadResult.ToResult();

                    var setResult = _recipeService.SetRecipeAndUpdateAttributes(loadResult.Value);
                    if (setResult.IsFailed)
                        return setResult;

                    // Any mutation resets consistency.
                    _state.SetRecipeConsistent(false);
                    UpdateAfterStructureChange();

                    return setResult.WithReasons(loadResult.Reasons);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unexpected error during load operation");
                    return Result.Fail(new ApplicationUnexpectedIoReadError());
                }
            },
            successMessage: $"Загружен рецепт из {Path.GetFileName(filePath)}",
            affectsRecipe: true);
    }

    public Task<Result> SaveRecipeAsync(string filePath)
    {
        return _pipeline.RunAsync(
            OperationId.Save,
            OperationKind.Saving,
            async () =>
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
            },
            successMessage: $"Рецепт сохранен в {Path.GetFileName(filePath)}",
            affectsRecipe: false);
    }

    public Task<Result> SendRecipeAsync()
    {
        return _pipeline.RunAsync(
            OperationId.Send,
            OperationKind.Transferring,
            async () =>
            {
                try
                {
                    if (!_state.GetSnapshot().EnaSendOk)
                    {
                        _logger.LogWarning("Send operation blocked by PLC logic (EnaSendOk is false)");
                        return Result.Fail(new ApplicationSendBlockedByPlcError());
                    }

                    var currentRecipe = _recipeService.CurrentRecipe;
                    var result = await _modbusTcpService.SendRecipeAsync(currentRecipe).ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        // Mark as consistent only on successful send.
                        _state.SetRecipeConsistent(true);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unexpected error during send operation");
                    return Result.Fail(new ApplicationUnexpectedIoWriteError());
                }
            },
            successMessage: "Рецепт успешно отправлен в контроллер",
            affectsRecipe: false);
    }

    public Task<Result> ReceiveRecipeAsync()
    {
        return _pipeline.RunAsync(
            OperationId.Receive,
            OperationKind.Transferring,
            async () =>
            {
                try
                {
                    var receiveResult = await _modbusTcpService.ReceiveRecipeAsync().ConfigureAwait(false);
                    _logger.LogTrace("Received recipe with {StepCount} steps from PLC",
                        receiveResult.IsSuccess && receiveResult.Value != null ? receiveResult.Value.Steps.Count : 0);

                    if (receiveResult.IsFailed || receiveResult.Value == null)
                        return receiveResult.ToResult();

                    var setResult = _recipeService.SetRecipeAndUpdateAttributes(receiveResult.Value);
                    if (setResult.IsSuccess)
                    {
                        // Any mutation resets consistency.
                        _state.SetRecipeConsistent(true);
                        UpdateAfterStructureChange();
                    }

                    return setResult.WithReasons(receiveResult.Reasons);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unexpected error during receive operation");
                    return Result.Fail(new ApplicationUnexpectedIoReadError());
                }
            },
            successMessage: "Рецепт успешно прочитан из контроллера",
            affectsRecipe: true);
    }

    private static Result ValidateRowIndex(int rowIndex, int rowCount)
    {
        return rowIndex < 0 || rowIndex > rowCount
            ? new ApplicationIndexOutOfRangeError(rowIndex, rowCount)
            : Result.Ok();
    }

    private Result ApplyPropertyUpdate(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        if (columnKey == MandatoryColumns.Action && value is short actionId)
            return _recipeService.ReplaceStepAction(rowIndex, actionId);

        return _recipeService.UpdateStepProperty(rowIndex, columnKey, value);
    }

    private void UpdateAfterCellChange(int rowIndex)
    {
        _timerControl.ResetForNewRecipe();
        ViewModel.OnTimeRecalculated(rowIndex);
        StepDataChanged?.Invoke(rowIndex);
    }

    private void UpdateAfterStructureChange()
    {
        _state.SetStepCount(_recipeService.StepCount);
        ViewModel.OnRecipeStructureChanged();
        RecipeStructureChanged?.Invoke();
        _timerControl.ResetForNewRecipe();
    }
}