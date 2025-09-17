#nullable enable

using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Presentation.Table;
using NtoLib.Recipes.MbeTable.Presentation.Table.Binding;
using NtoLib.Recipes.MbeTable.StateMachine;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;
using NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

/// <summary>
/// Acts as the main UI orchestrator for the recipe view. It owns the
/// UI-bound collection of view models and coordinates changes by delegating
/// business logic to application services.
/// </summary>
public sealed class RecipeViewModel
{
    /// <summary>
    /// Gets the collection of StepViewModels for data binding.
    /// </summary>
    public DynamicBindingList ViewModels { get; }

    private readonly IRecipeApplicationService _recipeService;
    private readonly IStepViewModelFactory _stepViewModelFactory;
    private readonly ILogger _debugLogger;
    private readonly IStatusManager _statusManager;
    private readonly AppStateMachine _appStateMachine;
    private readonly TimerService _timerService;

    private IUiDispatcher _uiDispatcher = new ImmediateUiDispatcher();
    private Recipe _recipe;

    /// <summary>
    /// Occurs when a view model update operation starts.
    /// </summary>
    public event Action? OnUpdateStart;

    /// <summary>
    /// Occurs when a view model update operation ends.
    /// </summary>
    public event Action? OnUpdateEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeViewModel"/> class.
    /// </summary>
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
        _recipeService = recipeService;
        _stepViewModelFactory = stepViewModelFactory;
        _appStateMachine = appStateMachine;
        _timerService = timerService;
        _statusManager = statusManager;
        _debugLogger = debugLogger;

        ViewModels = new DynamicBindingList(tableColumns, registry);
        _recipe = _recipeService.CreateEmpty().Recipe;
    }

    /// <summary>
    /// Sets the UI dispatcher to marshal calls to the UI thread.
    /// </summary>
    /// <param name="uiDispatcher">The UI dispatcher implementation.</param>
    public void SetUiDispatcher(IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcher();
        var initialResult = _recipeService.CreateEmpty();
        UpdateStateAndViewModels(initialResult);
    }

    /// <summary>
    /// Gets the current, immutable recipe domain object.
    /// </summary>
    /// <returns>The current <see cref="Recipe"/>.</returns>
    public Recipe GetCurrentRecipe() => _recipe;

    /// <summary>
    /// Replaces the entire recipe with a new one, e.g., from a file.
    /// </summary>
    /// <param name="newRecipe">The new recipe to display.</param>
    public void SetRecipe(Recipe newRecipe)
    {
        var newResult = _recipeService.AnalyzeRecipe(newRecipe);
        UpdateStateAndViewModels(newResult);
    }

    /// <summary>
    /// Adds a new default step at the specified index.
    /// </summary>
    /// <param name="rowIndex">The zero-based index at which to insert the new step.</param>
    public void AddNewStep(int rowIndex)
    {
        var clampedIndex = Math.Max(0, Math.Min(rowIndex, ViewModels.Count));
        var result = _recipeService.AddDefaultStep(_recipe, clampedIndex);
        UpdateStateAndViewModels(result, new StructuralChange(ChangeType.Add, clampedIndex));
    }

    /// <summary>
    /// Removes the step at the specified index.
    /// </summary>
    /// <param name="rowIndex">The zero-based index of the step to remove.</param>
    public void RemoveStep(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;
        var result = _recipeService.RemoveStep(_recipe, rowIndex);
        UpdateStateAndViewModels(result, new StructuralChange(ChangeType.Remove, rowIndex));
    }

    private void OnStepPropertyChanged(int rowIndex, ColumnIdentifier key, object value)
    {
        var result = (key == WellKnownColumns.Action && value is int newActionId)
            ? Result.Ok(_recipeService.ReplaceStepWithNewDefault(_recipe, rowIndex, newActionId))
            : _recipeService.UpdateStepProperty(_recipe, rowIndex, key, value);

        if (result.IsFailed)
        {
            var error = result.Errors.First();
            _debugLogger.Log(
                $"Step property update failed. Row: {rowIndex}, Key: {key.Value}, Value: '{value}'. Error: {error.Message}");
            _statusManager.WriteStatusMessage(error.Message, StatusMessage.Error);

            _uiDispatcher.Post(() => ViewModels.ResetItem(rowIndex));
            return;
        }

        _debugLogger.Log($"Step property updated. Row: {rowIndex}, Key: {key.Value}, Value: '{value}'");
        UpdateStateAndViewModels(result.Value, new StructuralChange(ChangeType.Update, rowIndex));
    }

    private void UpdateStateAndViewModels(RecipeUpdateResult result, StructuralChange? change = null)
    {
        _recipe = result.Recipe;
        _timerService.SetTimeAnalysisData(result.TimeResult);
        _appStateMachine.Dispatch(new VmLoopValidChanged(result.LoopResult.IsValid));

        _uiDispatcher.Post(() =>
        {
            OnUpdateStart?.Invoke();
            try
            {
                if (change == null)
                {
                    RebuildViewModelList(result);
                }
                else
                {
                    ApplyIncrementalUpdate(result, change);
                }
            }
            finally
            {
                _debugLogger.Log($"Current stepViewModel quantity {ViewModels.Count}");
                OnUpdateEnd?.Invoke();
            }
        });
    }

    private void RebuildViewModelList(RecipeUpdateResult result)
    {
        ViewModels.RaiseListChangedEvents = false;
        try
        {
            ViewModels.Clear();
            for (var i = 0; i < result.Recipe.Steps.Count; i++)
            {
                ViewModels.Add(_stepViewModelFactory.Create(result.Recipe.Steps[i], i, result, OnStepPropertyChanged));
            }
        }
        finally
        {
            ViewModels.RaiseListChangedEvents = true;
            ViewModels.ResetBindings();
        }
        _debugLogger.Log("ViewModel list was fully rebuilt.");
    }

    private void ApplyIncrementalUpdate(RecipeUpdateResult result, StructuralChange change)
    {
        // For structural changes, we perform the operation directly on the ViewModel.
        // The BindingList will efficiently notify the DataGridView to add/remove a single row.
        if (change.Type == ChangeType.Add)
        {
            var newVm = _stepViewModelFactory.Create(result.Recipe.Steps[change.Index], change.Index, result, OnStepPropertyChanged);
            ViewModels.Insert(change.Index, newVm);
        }
        else if (change.Type == ChangeType.Remove)
        {
            ViewModels.RemoveAt(change.Index);
        }

        // After any change (Add, Remove, or Update), the data in subsequent rows is stale.
        // We must update their internal state and then notify the UI to refresh only those rows.
        ViewModels.RaiseListChangedEvents = false;
        try
        {
            int startIndex = change.Index;
            for (int i = startIndex; i < ViewModels.Count; i++)
            {
                if (i >= result.Recipe.Steps.Count) break;

                result.TimeResult.StepStartTimes.TryGetValue(i, out var startTime);
                ViewModels[i].UpdateInPlace(result.Recipe.Steps[i], i, startTime);
            }
        }
        finally
        {
            ViewModels.RaiseListChangedEvents = true;
            // Now, tell the grid to refresh only the rows whose data we just changed.
            // This is fast because it doesn't redraw the whole table.
            for (int i = change.Index; i < ViewModels.Count; i++)
            {
                ViewModels.ResetItem(i);
            }
        }
    }

    private enum ChangeType { Add, Remove, Update }

    private sealed record StructuralChange(ChangeType Type, int Index);
}