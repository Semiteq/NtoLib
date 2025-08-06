#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeManager.Analysis;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Errors;
using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Status;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.RecipeManager.ViewModels
{
    /// <summary>
    /// Acts as the main controller and orchestrator for the recipe UI. It owns the
    /// complete state of the recipe view and coordinates all changes through the domain services.
    /// </summary>
    public sealed class RecipeViewModel : INotifyPropertyChanged
    {
        private readonly RecipeEngine _engine;
        private readonly RecipeLoopValidator _loopValidator;
        private readonly RecipeTimeCalculator _timeCalculator;
        private readonly ComboboxDataProvider _dataProvider;
        private readonly IStatusManager _statusManager;
        private readonly TableSchema _tableSchema;

        private Recipe _recipe;
        private LoopValidationResult _loopResult;
        private RecipeTimeAnalysis _timeResult;

        public event PropertyChangedEventHandler? PropertyChanged;
        public BindingList<StepViewModel> ViewModels { get; }

        public event Action? OnUpdateStart;
        public event Action? OnUpdateEnd;

        public RecipeViewModel(
            RecipeEngine engine,
            RecipeLoopValidator loopValidator,
            RecipeTimeCalculator timeCalculator,
            ComboboxDataProvider dataProvider,
            IStatusManager statusManager,
            TableSchema tableSchema)
        {
            _engine = engine;
            _loopValidator = loopValidator;
            _timeCalculator = timeCalculator;
            _dataProvider = dataProvider;
            _statusManager = statusManager;
            _tableSchema = tableSchema;

            ViewModels = new BindingList<StepViewModel>();

            _recipe = _engine.CreateEmptyRecipe();
            
            // Initial state calculation
            UpdateRecipeState(_recipe);
            RefreshViewModels();
        }

        public void AddNewStep(int rowIndex)
        {
            var newRecipe = _engine.AddDefaultStep(_recipe, rowIndex);
            // Structural change, a full refresh, is simplest and most reliable here.
            UpdateRecipeState(newRecipe);
            RefreshViewModels();
        }

        public void RemoveStep(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;
            var newRecipe = _engine.RemoveStep(_recipe, rowIndex);
            // Structural change, a full refresh, is simplest and most reliable here.
            UpdateRecipeState(newRecipe);
            RefreshViewModels();
        }

        private void OnStepPropertyChanged(int rowIndex, ColumnKey key, object value)
        {
            Debug.WriteLine($"Step {rowIndex} property {key} changed to {value}");
            Recipe newRecipe;
            RecipePropertyError? error;

            if (key == ColumnKey.Action && value is int newActionId)
            {
                newRecipe = _engine.ReplaceStepWithNewDefault(_recipe, rowIndex, newActionId);
                error = null; // Change step is valid by design
            }
            else
            {
                (newRecipe, error) = _engine.UpdateStepProperty(_recipe, rowIndex, key, value);
            }

            if (error != null)
            {
                // Error occurred (e.g., validation failed).
                // The recipe was not updated. We restore the UI to match the last valid state.
                _statusManager.WriteStatusMessage(error.Message, StatusMessage.Error);
                RefreshViewModels(); // Full refresh to guarantee consistency after error
                return;
            }

            // State was updated successfully. Now, update the UI intelligently.
            UpdateRecipeState(newRecipe);

            // A change in one step can affect the timing and nesting of all subsequent steps.
            // So we refresh this and all following ViewModels.
            // This is much faster than clearing and re-adding everything.
            bool isActionChange = key == ColumnKey.Action;
            RefreshViewModelsFrom(rowIndex, isActionChange);
        }

        /// <summary>
        /// Updates the core recipe state (data model) without touching the UI.
        /// </summary>
        private void UpdateRecipeState(Recipe newRecipe)
        {
            _recipe = newRecipe;

            _loopResult = _loopValidator.Validate(_recipe);
            _timeResult = _timeCalculator.Calculate(_recipe, _loopResult);

            if (!_loopResult.IsValid)
            {
                // Display error but don't block the update, the UI will reflect the invalid state.
                _statusManager.WriteStatusMessage(_loopResult.ErrorMessage!, StatusMessage.Error);
            }
        }
        
        /// <summary>
        /// Performs a full refresh of the ViewModels list. Used for initialization and major
        /// structural changes like Add/Remove Step.
        /// </summary>
        private void RefreshViewModels()
        {
            OnUpdateStart?.Invoke();
            Debug.WriteLine("ViewModels are fully updated");
            ViewModels.RaiseListChangedEvents = false;
            try
            {
                var allActionsCache = _dataProvider.GetActions();
                ViewModels.Clear();

                foreach (var (step, index) in _recipe.Steps.Select((s, i) => (s, i)))
                {
                    var vm = CreateStepViewModel(step, index, allActionsCache);
                    ViewModels.Add(vm);
                }
            }
            finally
            {
                ViewModels.RaiseListChangedEvents = true;
                ViewModels.ResetBindings();
            }
            OnUpdateEnd?.Invoke();
        }
        
        /// <summary>
        /// Efficiently refreshes the UI from a specific row downwards. This is the primary optimization
        /// to prevent flickering on data entry, as it avoids clearing the whole list.
        /// </summary>
        private void RefreshViewModelsFrom(int startIndex, bool actionChanged)
        {
            OnUpdateStart?.Invoke();
            Debug.WriteLine($"ViewModels are partially updated from index {startIndex}");
            ViewModels.RaiseListChangedEvents = false;
            try
            {
                var allActionsCache = _dataProvider.GetActions();

                for (int i = startIndex; i < _recipe.Steps.Count; i++)
                {
                    var step = _recipe.Steps[i];
                    
                    // If the action of the CURRENT step changed, we need new targets.
                    // For subsequent steps, we only need to update calculated values (time, nesting).
                    var availableTargets = (i == startIndex && actionChanged) 
                        ? _dataProvider.GetActionTargets(step.Properties[ColumnKey.Action]?.GetValue<int>() ?? 0)
                        : ViewModels[i].AvailableActionTargets;

                    var newVm = CreateStepViewModel(step, i, allActionsCache, availableTargets);
                    ViewModels[i] = newVm;
                }
            }
            finally
            {
                ViewModels.RaiseListChangedEvents = true;
                // ResetBindings is crucial to tell the DataGridView to repaint the changed rows.
                ViewModels.ResetBindings();
            }
            OnUpdateEnd?.Invoke();
        }

        /// <summary>
        /// Helper factory method to create a single StepViewModel instance.
        /// </summary>
        private StepViewModel CreateStepViewModel(Step step, int index, IReadOnlyDictionary<int, string> allActions, IReadOnlyDictionary<int, string>? availableTargets = null)
        {
            if (!_loopResult.NestingLevels.TryGetValue(index, out var nestingLevel))
            {
                nestingLevel = 0; // Default if not found
            }

            if (!_timeResult.StepStartTimes.TryGetValue(index, out var startTime))
            {
                startTime = TimeSpan.Zero; // Default if not found
            }

            if (availableTargets == null)
            {
                var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>() ?? 0;
                availableTargets = _dataProvider.GetActionTargets(actionId);
            }

            return new StepViewModel(
                step,
                _tableSchema,
                (key, val) => OnStepPropertyChanged(index, key, val),
                nestingLevel,
                startTime,
                allActions,
                availableTargets
            );
        }
    }
}