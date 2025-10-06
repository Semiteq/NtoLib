using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Application.Operations;
using NtoLib.Recipes.MbeTable.Application.Services;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.Application;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeService _recipeService;
    private readonly IModbusTcpService _modbusTcpService;
    private readonly ICsvService _csvOperations;
    private readonly IUiStateService _uiStateService;
    private readonly RecipeViewModel _viewModel;
    private readonly ILogger _logger;

    private int _operationInProgress;

    public RecipeViewModel ViewModel => _viewModel;

    public event Action? RecipeStructureChanged;
    public event Action<int>? StepDataChanged;
    public event Action<bool>? ValidationStateChanged
    {
        add => _recipeService.ValidationStateChanged += value;
        remove => _recipeService.ValidationStateChanged -= value;
    }

    public RecipeApplicationService(
        IRecipeService recipeService,
        IModbusTcpService modbusTcpService,
        ICsvService csvOperations,
        IUiStateService uiStateService,
        RecipeViewModel viewModel,
        ILogger logger)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _modbusTcpService = modbusTcpService ?? throw new ArgumentNullException(nameof(modbusTcpService));
        _csvOperations = csvOperations ?? throw new ArgumentNullException(nameof(csvOperations));
        _uiStateService = uiStateService ?? throw new ArgumentNullException(nameof(uiStateService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            var error = $"Invalid row index: {rowIndex}";
            _uiStateService.ShowError(error);
            return Result.Fail(error);
        }
        
        var isActionChange = columnKey == MandatoryColumns.Action && value is short;

        var result = isActionChange
            ? _recipeService.ReplaceStepAction(rowIndex, (short)value)
            : _recipeService.UpdateStepProperty(rowIndex, columnKey, value);

        if (result.IsSuccess)
        {
            _viewModel.OnStepDataChanged(rowIndex);
        }
        else
        {
            HandleOperationResult(result, "Failed to update cell value");
        }

        return result;
    }

    public Result AddStep(int index)
    {
        _logger.LogDebug($"Adding step at index {index}");
        
        var result = _recipeService.AddStep(index);
        
        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _uiStateService.ShowInfo($"Step added at position {index + 1}");
        }
        else
        {
            HandleOperationResult(result, "Failed to add step");
        }
        
        return result;
    }

    public Result RemoveStep(int index)
    {
        _logger.LogDebug($"Removing step at index {index}");
        
        var result = _recipeService.RemoveStep(index);
        
        if (result.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _uiStateService.ShowInfo($"Step removed at position {index + 1}");
        }
        else
        {
            HandleOperationResult(result, "Failed to remove step");
        }
        
        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        _logger.LogDebug($"LoadRecipeAsync called with path: {filePath}");
        
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            _logger.LogWarning("Operation already in progress");
            return Result.Fail("Operation already in progress");
        }

        _uiStateService.NotifyOperationStarted(OperationKind.Loading);

        try
        {
            var result = await LoadRecipeInternalAsync(filePath);
            
            if (result.IsSuccess)
            {
                _uiStateService.ShowInfo($"Recipe loaded from {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                HandleOperationResult(result, "Failed to load recipe");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while loading recipe. FilePath: {FilePath}", filePath);
            _uiStateService.ShowError($"Unexpected error: {ex.Message}");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
        finally
        {
            _uiStateService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> SaveRecipeAsync(string filePath)
    {
        _logger.LogDebug($"SaveRecipeAsync called with path: {filePath}");
        
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            _logger.LogWarning("Operation already in progress");
            return Result.Fail("Operation already in progress");
        }

        _uiStateService.NotifyOperationStarted(OperationKind.Saving);

        try
        {
            var result = await SaveRecipeInternalAsync(filePath);
            
            if (result.IsSuccess)
            {
                _uiStateService.ShowInfo($"Recipe saved to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                HandleOperationResult(result, "Failed to save recipe");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while saving recipe. FilePath: {FilePath}", filePath);
            _uiStateService.ShowError($"Unexpected error: {ex.Message}");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
        finally
        {
            _uiStateService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> SendRecipeAsync()
    {
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return Result.Fail("Operation already in progress");

        _uiStateService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await SendRecipeInternalAsync();
            
            if (result.IsSuccess)
            {
                _uiStateService.ShowInfo("Recipe sent to PLC successfully");
            }
            else
            {
                HandleOperationResult(result, "Failed to send recipe to PLC");
            }
            
            return result;
        }
        finally
        {
            _uiStateService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public async Task<Result> ReceiveRecipeAsync()
    {
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
            return Result.Fail("Operation already in progress");

        _uiStateService.NotifyOperationStarted(OperationKind.Transferring);

        try
        {
            var result = await ReceiveRecipeInternalAsync();
            
            if (result.IsSuccess)
            {
                _uiStateService.ShowInfo("Recipe received from PLC successfully");
            }
            else
            {
                HandleOperationResult(result, "Failed to receive recipe from PLC");
            }
            
            return result;
        }
        finally
        {
            _uiStateService.NotifyOperationCompleted();
            Interlocked.Exchange(ref _operationInProgress, 0);
        }
    }

    public int GetRowCount()
    {
        return _viewModel.GetRowCount();
    }

    private async Task<Result> LoadRecipeInternalAsync(string filePath)
    {
        _logger.LogDebug($"LoadRecipeInternalAsync: loading from {filePath}");
        
        var loadResult = await _csvOperations.ReadCsvAsync(filePath);
        if (loadResult.IsFailed)
        {
            _logger.LogError($"CSV read failed: {string.Join(", ", loadResult.Errors)}");
            return loadResult.ToResult();
        }

        var setResult = _recipeService.SetRecipe(loadResult.Value);
        
        if (setResult.IsSuccess)
        {
            _viewModel.OnRecipeStructureChanged();
            _logger.LogDebug($"Recipe loaded successfully, {loadResult.Value.Steps.Count} steps");
        }
        else
        {
            _logger.LogError($"SetRecipe failed: {string.Join(", ", setResult.Errors)}");
        }
        
        return setResult;
    }

    private async Task<Result> SaveRecipeInternalAsync(string filePath)
    {
        var currentRecipe = _recipeService.GetCurrentRecipe();
        _logger.LogDebug($"SaveRecipeInternalAsync: saving {currentRecipe.Steps.Count} steps to {filePath}");
        
        var result = await _csvOperations.WriteCsvAsync(currentRecipe, filePath);
        
        if (result.IsSuccess)
        {
            _logger.LogDebug("Recipe saved successfully");
        }
        else
        {
            _logger.LogError($"Save failed: {string.Join(", ", result.Errors)}");
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
        
        return setResult;
    }

    private void HandleOperationResult(Result result, string operationDescription)
    {
        if (result.IsFailed)
        {
            var errorMessages = string.Join("; ", result.Errors.Select(e => e.Message));
            var errorReasons = string.Join("; ", result.Reasons.Select(r => r.Message));
            var metadata = result.Reasons
                .SelectMany(r => r.Metadata)
                .Select(kvp => $"{kvp.Key}={kvp.Value}")
                .ToList();
            var metadataString = metadata.Any() ? $" | Metadata: {string.Join(", ", metadata)}" : string.Empty;
        
            var fullErrorMessage = $"{operationDescription}: {errorMessages} | Reasons: {errorReasons}{metadataString}";
        
            _logger.LogError(fullErrorMessage);
            _uiStateService.ShowError($"{operationDescription}: {errorMessages}");
        }
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