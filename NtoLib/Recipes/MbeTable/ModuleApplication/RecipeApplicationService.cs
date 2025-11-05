using System;
using System.IO;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations;
using NtoLib.Recipes.MbeTable.ModuleApplication.Services;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IStateProvider _state;
    private readonly RecipeViewModel _viewModel;
    private readonly ILogger<RecipeApplicationService> _logger;
    private readonly ResultResolver _resolver;
    private readonly ITimerControl _timerControl;

    public RecipeViewModel ViewModel => _viewModel;

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
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _timerControl = timerControl ?? throw new ArgumentNullException(nameof(timerControl));

        _recipeService.ValidationStateChanged += OnValidationStateChanged;

        OnValidationStateChanged(_recipeService.IsValid());
        _state.SetStepCount(_recipeService.GetCurrentRecipe().Steps.Count);
    }

    public Recipe GetCurrentRecipe() => _recipeService.GetCurrentRecipe();

    public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object? value)
    {
        if (value == null)
            return Result.Ok();

        var recipe = _recipeService.GetCurrentRecipe();
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
        {
            var error = Errors.IndexOutOfRange(rowIndex, recipe.Steps.Count);
            _resolver.Resolve(Result.Fail(error), "обновление ячейки");
            return Result.Fail(error);
        }

        var isActionChange = columnKey == MandatoryColumns.Action && value is short;
        var affectsTime = isActionChange ||
                          columnKey == MandatoryColumns.Task ||
                          columnKey == MandatoryColumns.StepDuration;

        var result = _recipeService.UpdateStepProperty(rowIndex, columnKey, value);
        if (isActionChange)
            result = _recipeService.ReplaceStepAction(rowIndex, (short)value);

        var status = result.GetStatus();
        if (!result.IsFailed)
        {
            if (affectsTime)
            {
                _timerControl.ResetForNewRecipe();
                _viewModel.OnTimeRecalculated(rowIndex);
            }
            else
            {
                _viewModel.OnStepDataChanged(rowIndex);
                StepDataChanged?.Invoke(rowIndex);
            }
        }

        if (status != ResultStatus.Success)
        {
            _resolver.Resolve(result, "обновление ячейки");
        }

        _logger.LogTrace("SetCellValueAsync completed for row {RowIndex}, column {ColumnKey} with status {Status}", 
            rowIndex, columnKey, status);
    
        return result;
    }

    public Result AddStep(int index)
    {
        var result = _recipeService.AddStep(index);

        if (result.IsSuccess)
        {
            _state.SetStepCount(_recipeService.GetCurrentRecipe().Steps.Count);
            _viewModel.OnRecipeStructureChanged();
            RecipeStructureChanged?.Invoke();
            _timerControl.ResetForNewRecipe();
        }

        _resolver.Resolve(result, "добавление строки", $"Добавлена строка №{index + 1}");

        return result;
    }

    public Result RemoveStep(int index)
    {
        var result = _recipeService.RemoveStep(index);

        if (result.IsSuccess)
        {
            _state.SetStepCount(_recipeService.GetCurrentRecipe().Steps.Count);
            _viewModel.OnRecipeStructureChanged();
            RecipeStructureChanged?.Invoke();
            _timerControl.ResetForNewRecipe();
        }

        _resolver.Resolve(result, "удаление строки", $"Удалена строка №{index + 1}");

        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        var gate = _state.BeginOperation(OperationKind.Loading, OperationId.Load);
        if (gate.GetStatus() != ResultStatus.Success)
        {
            _resolver.Resolve(gate, "загрузка рецепта");
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                var result = await LoadRecipeInternalAsync(filePath);
                _resolver.Resolve(result, "загрузка рецепта", $"Загружен рецепт из {Path.GetFileName(filePath)}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during load operation");
                var errorResult = ResultBox.Fail(Codes.IoReadError);
                _resolver.Resolve(errorResult, "загрузка рецепта");
                return errorResult;
            }
        }
    }

    public async Task<Result> SaveRecipeAsync(string filePath)
    {
        var gate = _state.BeginOperation(OperationKind.Saving, OperationId.Save);
        if (gate.GetStatus() != ResultStatus.Success)
        {
            _resolver.Resolve(gate, "сохранение рецепта");
            return gate.ToResult();
        }

        using (gate.Value)
        {
            try
            {
                var result = await SaveRecipeInternalAsync(filePath);
                _resolver.Resolve(result, "сохранение рецепта", $"Рецепт сохранен в {Path.GetFileName(filePath)}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error during save operation");
                var errorResult = ResultBox.Fail(Codes.IoWriteError);
                _resolver.Resolve(errorResult, "сохранение рецепта");
                return errorResult;
            }
        }
    }

    public async Task<Result> SendRecipeAsync()
    {
        var gate = _state.BeginOperation(OperationKind.Transferring, OperationId.Send);
        if (gate.GetStatus() != ResultStatus.Success)
        {
            _resolver.Resolve(gate, "отправка рецепта");
            return gate.ToResult();
        }

        using (gate.Value)
        {
            var result = await SendRecipeInternalAsync();
            _resolver.Resolve(result, "отправка рецепта", "Рецепт успешно отправлен в контроллер");
            return result;
        }
    }

    public async Task<Result> ReceiveRecipeAsync()
    {
        var gate = _state.BeginOperation(OperationKind.Transferring, OperationId.Receive);
        if (gate.GetStatus() != ResultStatus.Success)
        {
            _resolver.Resolve(gate, "чтение рецепта");
            return gate.ToResult();
        }

        using (gate.Value)
        {
            var result = await ReceiveRecipeInternalAsync();
            _resolver.Resolve(result, "чтение рецепта", "Рецепт успешно прочитан из контроллера");
            return result;
        }
    }

    public int GetRowCount() => _recipeService.GetCurrentRecipe().Steps.Count;

    private async Task<Result> LoadRecipeInternalAsync(string filePath)
    {
        var loadResult = await _csvOperations.ReadCsvAsync(filePath);
        if (loadResult.IsFailed) return loadResult.ToResult();

        var setResult = _recipeService.SetRecipeAndUpdateAttributes(loadResult.Value);
        if (setResult.GetStatus() is not ResultStatus.Success) return setResult;

        _state.SetStepCount(_recipeService.GetCurrentRecipe().Steps.Count);
        _viewModel.OnRecipeStructureChanged();
        RecipeStructureChanged?.Invoke();
        _timerControl.ResetForNewRecipe();

        return setResult.WithReasons(loadResult.Reasons);
    }

    private async Task<Result> SaveRecipeInternalAsync(string filePath)
    {
        var currentRecipe = _recipeService.GetCurrentRecipe();
        return await _csvOperations.WriteCsvAsync(currentRecipe, filePath);
    }

    private async Task<Result> SendRecipeInternalAsync()
    {
        if (!_state.GetSnapshot().EnaSendOk)
        {
            _logger.LogWarning("Send operation blocked by PLC logic (EnaSendOk is false)");
            var error = new BilingualError(
                "Send operation blocked by PLC logic",
                "Операция отправки заблокирована логикой ПЛК");
            return Result.Fail(error);
        }

        var currentRecipe = _recipeService.GetCurrentRecipe();
        return await _modbusTcpService.SendRecipeAsync(currentRecipe);
    }

    private async Task<Result> ReceiveRecipeInternalAsync()
    {
        var receiveResult = await _modbusTcpService.ReceiveRecipeAsync();
        _logger.LogTrace("Received recipe with {StepCount} steps from PLC", receiveResult.IsSuccess && receiveResult.Value != null ? receiveResult.Value.Steps.Count : 0);

        if (receiveResult.IsFailed)
            return receiveResult.ToResult();

        if (receiveResult.GetStatus() != ResultStatus.Success)
            return receiveResult.ToResult();

        var setResult = _recipeService.SetRecipeAndUpdateAttributes(receiveResult.Value);
        if (setResult.IsSuccess)
        {
            _state.SetStepCount(_recipeService.GetCurrentRecipe().Steps.Count);
            _viewModel.OnRecipeStructureChanged();
            RecipeStructureChanged?.Invoke();
            _timerControl.ResetForNewRecipe();
        }

        return setResult.WithReasons(receiveResult.Reasons);
    }

    private void OnValidationStateChanged(bool isValid)
    {
        _state.SetValidation(isValid);

        if (isValid && _recipeService.GetCurrentRecipe().Steps.Count > 0)
        {
            _resolver.Resolve(ResultBox.Ok(), "validation");
        }
    }
}