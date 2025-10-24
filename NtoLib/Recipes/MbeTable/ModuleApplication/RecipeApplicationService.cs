using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations;
using NtoLib.Recipes.MbeTable.ModuleApplication.Services;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IUiPermissionService _permissionService;
    private readonly RecipeViewModel _viewModel;
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
        ILogger<RecipeApplicationService> logger,
        ResultResolver resolver)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _modbusTcpService = modbusTcpService ?? throw new ArgumentNullException(nameof(modbusTcpService));
        _csvOperations = csvOperations ?? throw new ArgumentNullException(nameof(csvOperations));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        _viewModel.RowCountChanged += OnRowCountChanged;
        _viewModel.RowInvalidationRequested += OnRowInvalidationRequested;
    }

    public Recipe GetCurrentRecipe()
    {
        return _recipeService.GetCurrentRecipe();
    }

    public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object? value)
    {
        if (value == null)
            return Result.Ok();

        var recipe = _recipeService.GetCurrentRecipe();
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
        {
            return Result.Fail(new Error($"Invalid row index: {rowIndex}")
                .WithCode(Codes.CoreIndexOutOfRange));
        }
        
        var isActionChange = columnKey == MandatoryColumns.Action && value is short;

        var result = isActionChange
            ? _recipeService.ReplaceStepAction(rowIndex, (short)value)
            : _recipeService.UpdateStepProperty(rowIndex, columnKey, value);

        if (result.IsSuccess)
        {
            _viewModel.OnStepDataChanged(rowIndex);
        }

        _resolver.Resolve(result, new ResolveOptions(
            Operation: "Обновлена ячейка",
            SuccessMessage: null,
            SilentOnPureSuccess: true));

        return result;
    }

    public Result AddStep(int index)
    {
        _logger.LogDebug("Adding step at index {Index}", index);
        
        var result = _recipeService.AddStep(index);
        
        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        _resolver.Resolve(result, new ResolveOptions(
            Operation: "Добавление строки",
            SuccessMessage: $"Добавлена строка №{index + 1}",
            SilentOnPureSuccess: false));
        
        return result;
    }

    public Result RemoveStep(int index)
    {
        _logger.LogDebug("Removing step at index {Index}", index);
        
        var result = _recipeService.RemoveStep(index);
        
        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }

        _resolver.Resolve(result, new ResolveOptions(
            Operation: "Удаление строки",
            SuccessMessage: $"Удалена строка №{index + 1}",
            SilentOnPureSuccess: false));
        
        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        _logger.LogDebug("LoadRecipeAsync called with path: {FilePath}", filePath);
        
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            _logger.LogWarning("Operation already in progress");
            return Result.Fail(new Error("Operation already in progress")
                .WithCode(Codes.CoreInvalidOperation));
        }

        _permissionService.NotifyOperationStarted(OperationKind.Loading);

        try
        {
            var result = await LoadRecipeInternalAsync(filePath);
            _resolver.Resolve(result, new ResolveOptions(
                Operation: "Загрузка рецепта",
                SuccessMessage: $"Загружен рецепт из {System.IO.Path.GetFileName(filePath)}",
                SilentOnPureSuccess: false));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while loading recipe. FilePath: {FilePath}", filePath);
            var errorResult = Result.Fail(new Error($"Unexpected error: {ex.Message}")
                .WithCode(Codes.IoReadError));
            _resolver.Resolve(errorResult, new ResolveOptions("load recipe", null));
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
        _logger.LogDebug("SaveRecipeAsync called with path: {FilePath}", filePath);
        
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            _logger.LogWarning("Operation already in progress");
            return Result.Fail(new Error("Operation already in progress")
                .WithCode(Codes.CoreInvalidOperation));
        }

        _permissionService.NotifyOperationStarted(OperationKind.Saving);

        try
        {
            var result = await SaveRecipeInternalAsync(filePath);
            _resolver.Resolve(result, new ResolveOptions(
                Operation: "Сохранение рецепта",
                SuccessMessage: $"Рецепт сохранен в {System.IO.Path.GetFileName(filePath)}",
                SilentOnPureSuccess: false));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while saving recipe. FilePath: {FilePath}", filePath);
            var errorResult = Result.Fail(new Error($"Unexpected error: {ex.Message}")
                .WithCode(Codes.IoWriteError));
            _resolver.Resolve(errorResult, new ResolveOptions("save recipe", null));
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
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return Result.Fail(new Error("Operation already in progress")
                .WithCode(Codes.CoreInvalidOperation));

        _permissionService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await SendRecipeInternalAsync();
            _resolver.Resolve(result, new ResolveOptions(
                Operation: "Отправка рецепта",
                SuccessMessage: "Рецепт успешно отправлен",
                SilentOnPureSuccess: false));

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
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return Result.Fail(new Error("Operation already in progress")
                .WithCode(Codes.CoreInvalidOperation));

        _permissionService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await ReceiveRecipeInternalAsync();
            
            _resolver.Resolve(result, new ResolveOptions(
                Operation: "Чтение рецепта",
                SuccessMessage: "Рецепт успешно прочитан",
                SilentOnPureSuccess: false));

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

    private async Task<Result> LoadRecipeInternalAsync(string filePath)
    {
        var loadResult = await _csvOperations.ReadCsvAsync(filePath);
        if (loadResult.IsFailed)
        {
            _logger.LogError("CSV read failed: {Errors}", string.Join(", ", loadResult.Errors));
            return loadResult.ToResult();
        }

        var setResult = _recipeService.SetRecipe(loadResult.Value);
        
        if (setResult.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _logger.LogDebug("Recipe loaded successfully, {Steps} steps", loadResult.Value.Steps.Count);
        }
        else
        {
            _logger.LogError("SetRecipe failed: {Errors}", string.Join(", ", setResult.Errors));
        }
        
        return setResult.WithReasons(loadResult.Reasons);
    }

    private async Task<Result> SaveRecipeInternalAsync(string filePath)
    {
        var currentRecipe = _recipeService.GetCurrentRecipe();
        _logger.LogDebug("SaveRecipeInternalAsync: saving {Steps} steps to {FilePath}", currentRecipe.Steps.Count, filePath);
        
        var result = await _csvOperations.WriteCsvAsync(currentRecipe, filePath);
        
        if (result.IsSuccess)
        {
            _logger.LogDebug("Recipe saved successfully");
        }
        else
        {
            _logger.LogError("Save failed: {Errors}", string.Join(", ", result.Errors));
        }
        
        return result;
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

        var setResult = _recipeService.SetRecipe(receiveResult.Value);
        
        if (setResult.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
        }
        
        return setResult.WithReasons(receiveResult.Reasons);
    }

    private void OnRowCountChanged(int newCount)
    {
        RecipeStructureChanged?.Invoke();
    }

    private void OnRowInvalidationRequested(int rowIndex)
    {
        StepDataChanged?.Invoke(rowIndex);
    }
}