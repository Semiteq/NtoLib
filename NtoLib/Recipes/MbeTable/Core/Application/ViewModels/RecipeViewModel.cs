#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

/// <summary>
/// Main UI orchestrator for the recipe view. Manages collection of StepViewModels
/// and coordinates changes via application services. Works with VirtualMode DataGridView.
/// </summary>
public sealed class RecipeViewModel
{
    private readonly List<StepViewModel> _viewModels = new();
    private readonly TableColumns _tableColumns;

    public IReadOnlyList<StepViewModel> ViewModels => _viewModels;

    private readonly IRecipeApplicationService _recipeService;
    private readonly IStepViewModelFactory _stepViewModelFactory;
    private readonly ILogger _debugLogger;
    private readonly IStatusManager _statusManager;
    private readonly AppStateMachine _appStateMachine;
    private readonly TimerService _timerService;

    /// <summary>
    /// Occurs when a specific row needs to be invalidated (e.g., after Action change).
    /// </summary>
    public event Action<int>? RowInvalidationRequested;


    private bool _initialFullRedrawDone;
    private Action? _fullRedrawRequest;

    private IUiDispatcher _uiDispatcher = new ImmediateUiDispatcher();
    private Recipe _recipe;

    /// <summary>
    /// Occurs when the row count changes (add/remove). UI should update DataGridView.RowCount.
    /// </summary>
    public event Action<int>? RowCountChanged;

    /// <summary>
    /// Occurs when validation fails during SetCellValue. UI should display error and invalidate cell.
    /// </summary>
    public event Action<int, string>? ValidationFailed;

    public RecipeViewModel(
        IRecipeApplicationService recipeService,
        IStepViewModelFactory stepViewModelFactory,
        AppStateMachine appStateMachine,
        PropertyDefinitionRegistry registry,
        TimerService timerService,
        IStatusManager statusManager,
        ILogger debugLogger,
        TableColumns tableColumns)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _stepViewModelFactory = stepViewModelFactory ?? throw new ArgumentNullException(nameof(stepViewModelFactory));
        _appStateMachine = appStateMachine ?? throw new ArgumentNullException(nameof(appStateMachine));
        _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));

        _recipe = _recipeService.CreateEmpty().Recipe;
    }

    public void SetUiDispatcher(IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcher();
        var initialResult = _recipeService.CreateEmpty();
        UpdateStateAndViewModels(initialResult);
    }

    public Recipe GetCurrentRecipe() => _recipe;

    public void SetRecipe(Recipe newRecipe)
    {
        var newResult = _recipeService.AnalyzeRecipe(newRecipe);
        UpdateStateAndViewModels(newResult);
    }

    public void AddNewStep(int rowIndex)
    {
        var clampedIndex = Math.Max(0, Math.Min(rowIndex, _viewModels.Count));
        var result = _recipeService.AddDefaultStep(_recipe, clampedIndex);
        UpdateStateAndViewModels(result);
    }

    public void RemoveStep(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count) return;
        var result = _recipeService.RemoveStep(_recipe, rowIndex);
        UpdateStateAndViewModels(result);
    }

    /// <summary>
    /// Gets the display value for a cell. Called by VirtualMode CellValueNeeded.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <returns>Formatted value for display.</returns>
    public object? GetCellValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count)
        {
            return null;
        }

        var vm = _viewModels[rowIndex];
        var columnKey = _tableColumns.GetColumnDefinition(columnIndex).Key;
        var value = vm.GetPropertyValue(columnKey);

        return value;
    }

    /// <summary>
    /// Sets a cell value from user input. Called by VirtualMode CellValuePushed.
    /// Returns Result for validation feedback.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <param name="value">User-provided value.</param>
    /// <returns>Result indicating success or validation error.</returns>
    public Result SetCellValue(int rowIndex, int columnIndex, object? value)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count)
            return Result.Fail("Invalid row index");

        var columnKey = _tableColumns.GetColumnDefinition(columnIndex).Key;
        return OnStepPropertyChangedInternal(rowIndex, columnKey, value);
    }

    /// <summary>
    /// Returns current row count for DataGridView.RowCount.
    /// </summary>
    public int GetRowCount() => _viewModels.Count;

    private void OnStepPropertyChangedCallback(int rowIndex, ColumnIdentifier key, object value)
    {
        var result = OnStepPropertyChangedInternal(rowIndex, key, value);

        if (result.IsFailed)
        {
            ValidationFailed?.Invoke(rowIndex, result.Errors.First().Message);
        }
    }

    private Result OnStepPropertyChangedInternal(int rowIndex, ColumnIdentifier key, object value)
    {
        bool isActionChange = key == WellKnownColumns.Action && value is int;

        var serviceResult = isActionChange
            ? Result.Ok(_recipeService.ReplaceStepWithNewDefault(_recipe, rowIndex, (int)value))
            : _recipeService.UpdateStepProperty(_recipe, rowIndex, key, value);

        if (serviceResult.IsFailed)
        {
            var error = serviceResult.Errors.First();
            _statusManager.WriteStatusMessage(error.Message, StatusMessage.Error);
            return serviceResult.ToResult();
        }

        if (isActionChange)
        {
            UpdateStateAndViewModels(serviceResult.Value, () => RowInvalidationRequested?.Invoke(rowIndex));
        }
        else
        {
            UpdateStateAndViewModels(serviceResult.Value);
        }

        return Result.Ok();
    }

    private void UpdateStateAndViewModels(RecipeUpdateResult result, Action? afterUpdate = null)
    {
        _recipe = result.Recipe;
        _timerService.SetTimeAnalysisData(result.TimeResult);
        _appStateMachine.Dispatch(new VmLoopValidChanged(result.LoopResult.IsValid));

        _uiDispatcher.Post(() =>
        {
            if (_viewModels.Count != result.Recipe.Steps.Count)
                RebuildViewModelList(result);
            else
                UpdateExistingViewModels(result);

            RowCountChanged?.Invoke(_viewModels.Count);

            if (!_initialFullRedrawDone && _viewModels.Count > 0)
            {
                _initialFullRedrawDone = true;
                try
                {
                    _fullRedrawRequest?.Invoke();
                }
                catch
                {
                }
            }

            afterUpdate?.Invoke();
        });
    }

    private void RebuildViewModelList(RecipeUpdateResult result)
    {
        _viewModels.Clear();

        for (var i = 0; i < result.Recipe.Steps.Count; i++)
        {
            _viewModels.Add(_stepViewModelFactory.Create(
                result.Recipe.Steps[i], i, result, OnStepPropertyChangedCallback));
        }
    }

    private void UpdateExistingViewModels(RecipeUpdateResult result)
    {
        for (int i = 0; i < _viewModels.Count; i++)
        {
            if (i >= result.Recipe.Steps.Count) break;
            result.TimeResult.StepStartTimes.TryGetValue(i, out var startTime);
            _viewModels[i].UpdateInPlace(result.Recipe.Steps[i], i, startTime);
        }
    }
}