using System;
using System.Threading;
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
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IUiPermissionService _permissionService;
    private readonly RecipeViewModel _viewModel;
    private readonly ErrorDefinitionRegistry _errorRegistry;
    private readonly ILogger<RecipeApplicationService> _logger;
    private readonly ResultResolver _resolver;

    private int _operationInProgress;

    public RecipeViewModel ViewModel => _viewModel;

    public event Action? RecipeStructureChanged;
    public event Action<int>? StepDataChanged;

    public RecipeApplicationService(
        IRecipeService recipeService,
        IModbusTcpService modbusTcpService,
        ICsvService csvOperations,
        IUiPermissionService permissionService,
        RecipeViewModel viewModel,
        ErrorDefinitionRegistry errorRegistry,
        ILogger<RecipeApplicationService> logger,
        ResultResolver resolver)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _modbusTcpService = modbusTcpService ?? throw new ArgumentNullException(nameof(modbusTcpService));
        _csvOperations = csvOperations ?? throw new ArgumentNullException(nameof(csvOperations));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _errorRegistry = errorRegistry ?? throw new ArgumentNullException(nameof(errorRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        _viewModel.RowCountChanged += OnRowCountChanged;
        _viewModel.RowInvalidationRequested += OnRowInvalidationRequested;

        _recipeService.ValidationStateChanged += OnValidationStateChanged;
        _permissionService.NotifyValidationChanged(_recipeService.IsValid());
    }

    public Recipe GetCurrentRecipe()
    {
        return _recipeService.GetCurrentRecipe();
    }

    public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object? value)
    {
        if (value == null)
            return ResultBox.Ok();

        var recipe = _recipeService.GetCurrentRecipe();
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
            return ResultBox.Fail(Codes.CoreIndexOutOfRange);

        var isActionChange = columnKey == MandatoryColumns.Action && value is short;
        var affectsTime = isActionChange || 
                          columnKey == MandatoryColumns.Task || 
                          columnKey == MandatoryColumns.StepDuration;

        var result = isActionChange
            ? _recipeService.ReplaceStepAction(rowIndex, (short)value)
            : _recipeService.UpdateStepProperty(rowIndex, columnKey, value);

        if (result.IsSuccess)
        {
            if (affectsTime)
            {
                _viewModel.OnTimeRecalculated(rowIndex);
            }
            else
            {
                _viewModel.OnStepDataChanged(rowIndex);
            }
        }

        if (result.IsFailed || result.GetStatus() == ResultStatus.Warning)
        {
            _resolver.Resolve(result, "обновление ячейки");
        }

        return result;
    }

    public Result AddStep(int index)
    {
        var result = _recipeService.AddStep(index);

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        _resolver.Resolve(result, "добавление строки", $"Добавлена строка №{index + 1}");

        return result;
    }

    public Result RemoveStep(int index)
    {
        var result = _recipeService.RemoveStep(index);

        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        _resolver.Resolve(result, "удаление строки", $"Удалена строка №{index + 1}");

        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        var guardResult = GuardOperationStart();
        if (guardResult.IsFailed)
        {
            _resolver.Resolve(guardResult, "загрузка рецепта");
            return guardResult;
        }

        _permissionService.NotifyOperationStarted(OperationKind.Loading);

        try
        {
            var result = await LoadRecipeInternalAsync(filePath);
            _resolver.Resolve(result, "загрузка рецепта", $"Загружен рецепт из {System.IO.Path.GetFileName(filePath)}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while loading recipe from {FilePath}", filePath);
            var errorResult = ResultBox.Fail(Codes.IoReadError);
            _resolver.Resolve(errorResult, "загрузка рецепта");
            return errorResult;
        }
        finally
        {
            _permissionService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> SaveRecipeAsync(string filePath)
    {
        var guardResult = GuardCanSave();
        if (guardResult.IsFailed)
        {
            _resolver.Resolve(guardResult, "сохранение рецепта");
            return guardResult;
        }

        _permissionService.NotifyOperationStarted(OperationKind.Saving);

        try
        {
            var result = await SaveRecipeInternalAsync(filePath);
            _resolver.Resolve(result, "сохранение рецепта", $"Рецепт сохранен в {System.IO.Path.GetFileName(filePath)}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while saving recipe to {FilePath}", filePath);
            var errorResult = ResultBox.Fail(Codes.IoWriteError);
            _resolver.Resolve(errorResult, "сохранение рецепта");
            return errorResult;
        }
        finally
        {
            _permissionService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> SendRecipeAsync()
    {
        var guardResult = GuardCanSend();
        if (guardResult.IsFailed)
        {
            _resolver.Resolve(guardResult, "отправка рецепта");
            return guardResult;
        }

        _permissionService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await SendRecipeInternalAsync();
            _resolver.Resolve(result, "отправка рецепта", "Рецепт успешно отправлен в контроллер");

            return result;
        }
        finally
        {
            _permissionService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> ReceiveRecipeAsync()
    {
        var guardResult = GuardOperationStart();
        if (guardResult.IsFailed)
        {
            _resolver.Resolve(guardResult, "чтение рецепта");
            return guardResult;
        }

        _permissionService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await ReceiveRecipeInternalAsync();
            _resolver.Resolve(result, "чтение рецепта", "Рецепт успешно прочитан из контроллера");

            return result;
        }
        finally
        {
            _permissionService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public int GetRowCount()
    {
        return _viewModel.GetRowCount();
    }

    private Result GuardOperationStart()
    {
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return ResultBox.Fail(Codes.CoreInvalidOperation);

        return ResultBox.Ok();
    }

    private Result GuardCanSave()
    {
        if (!_recipeService.IsValid())
            return ResultBox.Fail(Codes.CoreForLoopError);

        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return ResultBox.Fail(Codes.CoreInvalidOperation);

        return ResultBox.Ok();
    }

    private Result GuardCanSend()
    {
        if (!_recipeService.IsValid())
            return ResultBox.Fail(Codes.CoreForLoopError);

        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return ResultBox.Fail(Codes.CoreInvalidOperation);

        return ResultBox.Ok();
    }

    private async Task<Result> LoadRecipeInternalAsync(string filePath)
    {
        var loadResult = await _csvOperations.ReadCsvAsync(filePath);
        if (loadResult.IsFailed)
            return loadResult.ToResult();

        var setResult = _recipeService.SetRecipe(loadResult.Value);

        if (setResult.GetStatus() == ResultStatus.Warning &&
            setResult.TryGetCode(out var code) &&
            _errorRegistry.Blocks(code, BlockingScope.Load))
        {
            return ResultBox.Fail(code);
        }

        if (setResult.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        return setResult.WithReasons(loadResult.Reasons);
    }

    private async Task<Result> SaveRecipeInternalAsync(string filePath)
    {
        var currentRecipe = _recipeService.GetCurrentRecipe();
        return await _csvOperations.WriteCsvAsync(currentRecipe, filePath);
    }

    private async Task<Result> SendRecipeInternalAsync()
    {
        var currentRecipe = _recipeService.GetCurrentRecipe();
        return await _modbusTcpService.SendRecipeAsync(currentRecipe);
    }

    private async Task<Result> ReceiveRecipeInternalAsync()
    {
        var receiveResult = await _modbusTcpService.ReceiveRecipeAsync();
        if (receiveResult.IsFailed)
            return receiveResult.ToResult();

        if (receiveResult.GetStatus() == ResultStatus.Warning &&
            receiveResult.TryGetCode(out var code) &&
            _errorRegistry.Blocks(code, BlockingScope.Load))
        {
            return ResultBox.Fail(code);
        }

        var setResult = _recipeService.SetRecipe(receiveResult.Value);

        if (setResult.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        return setResult;
    }

    private void OnRowCountChanged(int newCount)
    {
        RecipeStructureChanged?.Invoke();
    }

    private void OnRowInvalidationRequested(int rowIndex)
    {
        StepDataChanged?.Invoke(rowIndex);
    }

    private void OnValidationStateChanged(bool isValid)
    {
        _permissionService.NotifyValidationChanged(isValid);
    }
}