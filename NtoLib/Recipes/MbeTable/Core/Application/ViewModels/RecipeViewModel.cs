#nullable enable

using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Application.Services;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
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
    public DynamicBindingList ViewModels { get; }

    private readonly IRecipeApplicationService _recipeService;
    private readonly IStepViewModelFactory _stepViewModelFactory;
    private readonly ILogger _debugLogger;
    private readonly IStatusManager _statusManager;
    private readonly AppStateMachine _appStateMachine;
    private readonly TimerService _timerService;

    private IUiDispatcher _uiDispatcher = new ImmediateUiDispatcher();
    private Recipe _recipe;

    public event Action? OnUpdateStart;
    public event Action? OnUpdateEnd;

    public RecipeViewModel(
        IRecipeApplicationService recipeService,
        IStepViewModelFactory stepViewModelFactory,
        AppStateMachine appStateMachine,
        TimerService timerService,
        IStatusManager statusManager,
        ILogger debugLogger,
        TableSchema tableSchema)
    {
        _recipeService = recipeService;
        _stepViewModelFactory = stepViewModelFactory;
        _appStateMachine = appStateMachine;
        _timerService = timerService;
        _statusManager = statusManager;
        _debugLogger = debugLogger;

        ViewModels = new DynamicBindingList(tableSchema);

        var initialResult = _recipeService.CreateEmpty();
        _recipe = initialResult.Recipe;
    }

    public void SetUiDispatcher(IUiDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcher();
        // Populate with initial empty recipe state
        var initialResult = _recipeService.CreateEmpty();
        UpdateStateAndViewModels(initialResult);
    }

    #region Public Commands

    public Recipe GetCurrentRecipe() => _recipe;

    public void SetRecipe(Recipe newRecipe)
    {
        // Here we should re-analyze the recipe to get consistent state
        var newResult = new RecipeUpdateResult(newRecipe, new Domain.Analysis.LoopValidationResult(),
            new Domain.Analysis.RecipeTimeAnalysis()); // Simplified for now, better to call a service method
        UpdateStateAndViewModels(newResult);
    }

    public void AddNewStep(int rowIndex)
    {
        rowIndex = Math.Max(0, Math.Min(rowIndex, ViewModels.Count));
        var result = _recipeService.AddDefaultStep(_recipe, rowIndex);
        UpdateStateAndViewModels(result, new StructuralChange(ChangeType.Add, rowIndex));
    }

    public void RemoveStep(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;
        var result = _recipeService.RemoveStep(_recipe, rowIndex);
        UpdateStateAndViewModels(result, new StructuralChange(ChangeType.Remove, rowIndex));
    }

    #endregion

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

            _uiDispatcher.Post(() =>
            {
                try
                {
                    ViewModels.ResetItem(rowIndex);
                }
                catch
                {
                    /* ignore */
                }
            });
            return;
        }

        _debugLogger.Log($"Step property updated. Row: {rowIndex}, Key: {key.Value}, Value: '{value}'");
        UpdateStateAndViewModels(result.Value, new StructuralChange(ChangeType.Update, rowIndex));
    }

    private void UpdateStateAndViewModels(RecipeUpdateResult result, StructuralChange? change = null)
    {
        // Update domain state
        _recipe = result.Recipe;
        _timerService.SetTimeAnalysisData(result.TimeResult);
        _appStateMachine.Dispatch(new VmLoopValidChanged(result.LoopResult.IsValid));

        // Perform all ViewModels mutations on UI thread
        _uiDispatcher.Post(() =>
        {
            OnUpdateStart?.Invoke();
            try
            {
                if (change == null || (ViewModels.Count != _recipe.Steps.Count && change.Type != ChangeType.Add))
                {
                    RebuildViewModelList(result);
                }
                else
                {
                    ApplyPartialUpdate(result, change);
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
            foreach (var (step, index) in result.Recipe.Steps.Select((s, i) => (s, i)))
            {
                ViewModels.Add(_stepViewModelFactory.Create(step, index, result, OnStepPropertyChanged));
            }
        }
        finally
        {
            ViewModels.RaiseListChangedEvents = true;
            ViewModels.ResetBindings();
        }

        _debugLogger.Log("ViewModel was repopulated.");
    }

    private void ApplyPartialUpdate(RecipeUpdateResult result, StructuralChange change)
    {
        switch (change.Type)
        {
            case ChangeType.Add:
                var newVm = _stepViewModelFactory.Create(result.Recipe.Steps[change.Index], change.Index, result,
                    OnStepPropertyChanged);
                ViewModels.Insert(change.Index, newVm);
                UpdateSubsequentViewModels(result, change.Index + 1);
                break;
            case ChangeType.Remove:
                ViewModels.RemoveAt(change.Index);
                UpdateSubsequentViewModels(result, change.Index);
                break;
            case ChangeType.Update:
                // Only need to update the affected and subsequent items
                UpdateSubsequentViewModels(result, change.Index);
                break;
        }
    }

    private void UpdateSubsequentViewModels(RecipeUpdateResult result, int startIndex)
    {
        ViewModels.RaiseListChangedEvents = false;
        try
        {
            for (int i = startIndex; i < result.Recipe.Steps.Count; i++)
            {
                ViewModels[i] = _stepViewModelFactory.Create(result.Recipe.Steps[i], i, result, OnStepPropertyChanged);
            }
        }
        finally
        {
            ViewModels.RaiseListChangedEvents = true;
            ViewModels.ResetBindings();
        }
    }

    #region Helper Types

    private enum ChangeType
    {
        Add,
        Remove,
        Update
    }

    private record StructuralChange(ChangeType Type, int Index);

    #endregion
}