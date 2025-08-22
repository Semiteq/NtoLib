#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Composition.StateMachine;
using NtoLib.Recipes.MbeTable.Core.Domain;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;
using NtoLib.Recipes.MbeTable.Presentation.Status;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels
{
    /// <summary>
    /// Acts as the main controller and orchestrator for the recipe UI. It owns the
    /// complete state of the recipe view and coordinates all changes through the domain services.
    /// </summary>
    public sealed class RecipeViewModel
    {
        public BindingList<StepViewModel> ViewModels { get; } = new();

        private readonly IRecipeEngine _recipeEngine;
        private readonly RecipeLoopValidator _recipeLoopValidator;
        private readonly RecipeTimeCalculator _recipeTimeCalculator;
        private readonly IComboboxDataProvider _comboboxDataProvider;
        private readonly DebugLogger _debugLogger;
        private readonly IStatusManager _statusManager;
        private readonly AppStateMachine _appStateMachine;

        private IUiDispatcher _uiDispatcher = new ImmediateUiDispatcher();
        
        private Recipe _recipe;

        private LoopValidationResult _loopResult = new();
        private RecipeTimeAnalysis _timeResult = new();

        public event Action? OnUpdateStart;
        public event Action? OnUpdateEnd;

        public RecipeViewModel(
            IRecipeEngine recipeEngine,
            RecipeLoopValidator recipeLoopValidator,
            RecipeTimeCalculator recipeTimeCalculator,
            IComboboxDataProvider comboboxDataProvider,
            IStatusManager statusManager,
            DebugLogger debugLogger,
            AppStateMachine appStateMachine)
        {
            _recipeEngine = recipeEngine;
            _recipeLoopValidator = recipeLoopValidator;
            _recipeTimeCalculator = recipeTimeCalculator;
            _comboboxDataProvider = comboboxDataProvider;
            _statusManager = statusManager;
            _debugLogger = debugLogger;
            _appStateMachine = appStateMachine;

            _recipe = _recipeEngine.CreateEmptyRecipe();
            // Initial populate must run on UI thread as soon as dispatcher is attached by TableControl.
            // For now just compute, actual ViewModels binding will be updated after dispatcher is set.
        }

        /// <summary>
        /// Attaches UI dispatcher that marshals list updates to the UI thread.
        /// Should be called from TableControl after it is created.
        /// </summary>
        public void SetUiDispatcher(IUiDispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcher();
            // Populate current recipe into ViewModels once dispatcher is ready
            UpdateRecipeStateAndViewModels(_recipe);
        }

        #region Public Commands

        public Recipe GetCurrentRecipe() => _recipe;
        
        // For EffectsHandler to set recipe after load
        public void SetRecipe(Recipe newRecipe)
        {
            UpdateRecipeStateAndViewModels(newRecipe);
        }

        public void AddNewStep(int rowIndex)
        {
            rowIndex = Math.Max(0, Math.Min(rowIndex, ViewModels.Count));
            var newRecipe = _recipeEngine.AddDefaultStep(_recipe, rowIndex);
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Add, rowIndex));
        }

        public void RemoveStep(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;
            var newRecipe = _recipeEngine.RemoveStep(_recipe, rowIndex);
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Remove, rowIndex));
        }

        #endregion

        #region Private Methods

        private void OnStepPropertyChanged(int rowIndex, ColumnKey key, object value)
        {
            Recipe newRecipe;
            RecipePropertyError? error;

            if (key == ColumnKey.Action && value is int newActionId)
            {
                newRecipe = _recipeEngine.ReplaceStepWithNewDefault(_recipe, rowIndex, newActionId);
                error = null;
            }
            else
            {
                (newRecipe, error) = _recipeEngine.UpdateStepProperty(_recipe, rowIndex, key, value);
            }

            if (error != null)
            {
                _debugLogger.Log($"Step property update failed. Row: {rowIndex}, Key: {key}, Value: '{value}'. Error: {error.Message}");
                _statusManager.WriteStatusMessage(error.Message, StatusMessage.Error);

                // UI-bound list reset must run on UI thread
                _uiDispatcher.Post(() =>
                {
                    try { ViewModels.ResetItem(rowIndex); } catch { /* ignore */ }
                });
                return;
            }

            _debugLogger.Log($"Step property updated. Row: {rowIndex}, Key: {key}, Value: '{value}'");
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Update, rowIndex));
        }

        private void UpdateRecipeStateAndViewModels(Recipe newRecipe, StructuralChange? change = null)
        {
            // Compute domain results off-UI thread (safe)
            _recipe = newRecipe;
            _loopResult = _recipeLoopValidator.Validate(_recipe);
            _timeResult = _recipeTimeCalculator.Calculate(_recipe, _loopResult);

            _appStateMachine.Dispatch(new VmLoopValidChanged(_loopResult.IsValid));

            // Now perform all ViewModels mutations on UI thread
            _uiDispatcher.Post(() =>
            {
                OnUpdateStart?.Invoke();
                try
                {
                    if (change == null || (ViewModels.Count != _recipe.Steps.Count && change.Type != ChangeType.Add))
                    {
                        // Full repopulate
                        ViewModels.RaiseListChangedEvents = false;
                        try
                        {
                            ViewModels.Clear();
                            foreach (var (step, index) in _recipe.Steps.Select((s, i) => (s, i)))
                            {
                                ViewModels.Add(CreateStepViewModel(step, index));
                            }
                        }
                        finally
                        {
                            ViewModels.RaiseListChangedEvents = true;
                        }
                        ViewModels.ResetBindings();
                        _debugLogger.Log("ViewModel was repopulated.");
                    }
                    else
                    {
                        // Partial update
                        switch (change.Type)
                        {
                            case ChangeType.Add:
                                var newVm = CreateStepViewModel(_recipe.Steps[change.Index], change.Index);
                                ViewModels.Insert(change.Index, newVm);
                                UpdateSubsequentViewModels(change.Index + 1);
                                break;

                            case ChangeType.Remove:
                                ViewModels.RemoveAt(change.Index);
                                UpdateSubsequentViewModels(change.Index);
                                break;

                            case ChangeType.Update:
                                UpdateSubsequentViewModels(change.Index);
                                break;
                        }
                    }
                }
                finally
                {
                    _debugLogger.Log($"Current stepViewModel quantity {ViewModels.Count}");
                    OnUpdateEnd?.Invoke();
                }
            });
        }

        private void UpdateSubsequentViewModels(int startIndex)
        {
            ViewModels.RaiseListChangedEvents = false;

            for (int i = startIndex; i < _recipe.Steps.Count; i++)
            {
                ViewModels[i] = CreateStepViewModel(_recipe.Steps[i], i);
            }

            ViewModels.RaiseListChangedEvents = true;

            ViewModels.ResetBindings();
        }

        private StepViewModel CreateStepViewModel(Step step, int index)
        {
            _loopResult.NestingLevels.TryGetValue(index, out var nestingLevel);
            _timeResult.StepStartTimes.TryGetValue(index, out var startTime);

            var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>();

            if (!actionId.HasValue)
            {
                var ex = new InvalidOperationException($"Step №{index} does not have an action.");
                _debugLogger.LogException(ex);
                throw ex;
            }

            var availableTargets = _comboboxDataProvider.GetActionTargets(actionId.Value);

            return new StepViewModel(
                step,
                (key, val) => OnStepPropertyChanged(index, key, val),
                startTime,
                availableTargets
            );
        }

        private static string MultilineErrors(IImmutableList<RecipeFileError> errors)
        {
            return string.Join(Environment.NewLine, 
                errors.Select((error, index) => $"[{index + 1}/{errors.Count}] {error}"));
        }

        #endregion

        #region Helper Types

        private enum ChangeType { Add, Remove, Update }
        private record StructuralChange(ChangeType Type, int Index);

        #endregion
    }
}