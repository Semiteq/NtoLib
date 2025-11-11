using System;
using System.IO;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations;
using NtoLib.Recipes.MbeTable.ModuleApplication.Services;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private const string OpCellUpdate = "обновление ячейки";
    private const string OpLoad = "загрузка рецепта";
    private const string OpSave = "сохранение рецепта";
    private const string OpSend = "отправка рецепта";
    private const string OpReceive = "чтение рецепта";

    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IStateProvider _state;
    private readonly ILogger<RecipeApplicationService> _logger;
    private readonly ResultResolver _resolver;
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
        ResultResolver resolver,
        ITimerControl timerControl)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _modbusTcpService = modbusTcpService ?? throw new ArgumentNullException(nameof(modbusTcpService));
        _csvOperations = csvOperations ?? throw new ArgumentNullException(nameof(csvOperations));
        _state = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _timerControl = timerControl ?? throw new ArgumentNullException(nameof(timerControl));

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _recipeService.ValidationStateChanged += OnValidationStateChanged;

        OnValidationStateChanged(_recipeService.IsValid());
        _state.SetStepCount(_recipeService.StepCount);
    }

    public Recipe GetCurrentRecipe() => _recipeService.CurrentRecipe;

    public int GetRowCount() => _recipeService.StepCount;
    
    public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        _resolver.Clear();

        var validation = ValidateRowIndex(rowIndex, _recipeService.StepCount);
        if (validation.IsFailed)
        {
            _resolver.Resolve(validation, OpCellUpdate);
            return validation;
        }

        var result = ApplyPropertyUpdate(rowIndex, columnKey, value);
        if (result.IsSuccess)
        {
            UpdateAfterCellChange(rowIndex);
        }

        _resolver.Resolve(result, OpCellUpdate);

        _logger.LogTrace(
            "SetCellValueAsync completed for row {RowIndex}, column {ColumnKey} with value {Value}. Status: {Status}.",
            rowIndex,
            columnKey,
            value,
            result.IsSuccess ? "OK" : "FAILED");

        return result;
    }

    public Result AddStep(int index)
    {
        var result = _recipeService.AddStep(index);

        if (result.IsSuccess)
        {
            UpdateAfterStructureChange();
        }

        _resolver.Resolve(result, "добавление строки", $"Добавлена строка №{index + 1}");
        return result;
    }

    public Result RemoveStep(int index)
    {
        var result = _recipeService.RemoveStep(index);

        if (result.IsSuccess)
        {
            UpdateAfterStructureChange();
        }

        _resolver.Resolve(result, "удаление строки", $"Удалена строка №{index + 1}");
        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        var gate = _state.BeginOperation(OperationKind.Loading, OperationId.Load);
        if (gate.IsFailed)
        {
            _resolver.Resolve(gate, OpLoad);
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                _resolver.Clear();
                var result = await LoadRecipeInternalAsync(filePath).ConfigureAwait(false);
                _resolver.Resolve(result, OpLoad, $"Загружен рецепт из {Path.GetFileName(filePath)}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during load operation");
                var error = new ApplicationUnexpectedIoReadError();
                _resolver.Resolve(error, OpLoad);
                return error;
            }
        }
    }

    public async Task<Result> SaveRecipeAsync(string filePath)
    {
        var gate = _state.BeginOperation(OperationKind.Saving, OperationId.Save);
        if (gate.IsFailed)
        {
            _resolver.Resolve(gate, OpSave);
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                _resolver.Clear();
                var result = await SaveRecipeInternalAsync(filePath).ConfigureAwait(false);
                _resolver.Resolve(result, OpSave, $"Рецепт сохранен в {Path.GetFileName(filePath)}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during save operation");
                var error = new ApplicationUnexpectedIoWriteError();
                _resolver.Resolve(error, OpSave);
                return error;
            }
        }
    }

    public async Task<Result> SendRecipeAsync()
    {
        var gate = _state.BeginOperation(OperationKind.Transferring, OperationId.Send);
        if (gate.IsFailed)
        {
            _resolver.Resolve(gate, OpSend);
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                _resolver.Clear();
                var result = await SendRecipeInternalAsync().ConfigureAwait(false);
                _resolver.Resolve(result, OpSend, "Рецепт успешно отправлен в контроллер");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during send operation");
                var error = new ApplicationUnexpectedIoWriteError();
                _resolver.Resolve(error, OpSend);
                return error;
            }
        }
    }

    public async Task<Result> ReceiveRecipeAsync()
    {
        var gate = _state.BeginOperation(OperationKind.Transferring, OperationId.Receive);
        if (gate.IsFailed)
        {
            _resolver.Resolve(gate, OpReceive);
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                _resolver.Clear();
                var result = await ReceiveRecipeInternalAsync().ConfigureAwait(false);
                _resolver.Resolve(result, OpReceive, "Рецепт успешно прочитан из контроллера");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during receive operation");
                var error = new ApplicationUnexpectedIoReadError();
                _resolver.Resolve(error, OpReceive);
                return error;
            }
        }
    }
    
    private static Result ValidateRowIndex(int rowIndex, int rowCount)
    {
        return rowIndex < 0 || rowIndex >= rowCount
            ? new ApplicationIndexOutOfRangeError(rowIndex, rowCount)
            : Result.Ok();
    }

    private Result ApplyPropertyUpdate(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        if (columnKey == MandatoryColumns.Action && value is short actionId)
            return _recipeService.ReplaceStepAction(rowIndex, actionId);

        return _recipeService.UpdateStepProperty(rowIndex, columnKey, value);
    }

    private async Task<Result> LoadRecipeInternalAsync(string filePath)
    {
        var loadResult = await _csvOperations.ReadCsvAsync(filePath).ConfigureAwait(false);
        if (loadResult.IsFailed)
            return loadResult.ToResult();

        var setResult = _recipeService.SetRecipeAndUpdateAttributes(loadResult.Value);
        if (setResult.IsFailed)
            return setResult;

        UpdateAfterStructureChange();

        return setResult.WithReasons(loadResult.Reasons);
    }

    private Task<Result> SaveRecipeInternalAsync(string filePath)
    {
        var currentRecipe = _recipeService.CurrentRecipe;
        return _csvOperations.WriteCsvAsync(currentRecipe, filePath);
    }

    private async Task<Result> SendRecipeInternalAsync()
    {
        if (!_state.GetSnapshot().EnaSendOk)
        {
            _logger.LogWarning("Send operation blocked by PLC logic (EnaSendOk is false)");
            return Result.Fail(new ApplicationSendBlockedByPlcError());
        }

        var currentRecipe = _recipeService.CurrentRecipe;
        return await _modbusTcpService.SendRecipeAsync(currentRecipe).ConfigureAwait(false);
    }

    private async Task<Result> ReceiveRecipeInternalAsync()
    {
        var receiveResult = await _modbusTcpService.ReceiveRecipeAsync().ConfigureAwait(false);
        _logger.LogTrace("Received recipe with {StepCount} steps from PLC",
            receiveResult.IsSuccess && receiveResult.Value != null ? receiveResult.Value.Steps.Count : 0);

        if (receiveResult.IsFailed || receiveResult.Value == null)
            return receiveResult.ToResult();

        var setResult = _recipeService.SetRecipeAndUpdateAttributes(receiveResult.Value);
        if (setResult.IsSuccess)
            UpdateAfterStructureChange();

        return setResult.WithReasons(receiveResult.Reasons);
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

    private void OnValidationStateChanged(bool isValid)
    {
        _state.SetValidation(isValid);

        if (isValid && _recipeService.StepCount > 0)
        {
            _resolver.Resolve(Result.Ok(), "validation");
        }
    }
}