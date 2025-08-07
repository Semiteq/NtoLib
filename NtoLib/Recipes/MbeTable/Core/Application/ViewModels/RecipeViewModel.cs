#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Status;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Application.ViewModels
{
    /// <summary>
    /// Acts as the main controller and orchestrator for the recipe UI. It owns the
    /// complete state of the recipe view and coordinates all changes through the domain services.
    /// </summary>
    public sealed class RecipeViewModel
    {
        private readonly RecipeEngine _engine;
        private readonly RecipeLoopValidator _loopValidator;
        private readonly RecipeTimeCalculator _timeCalculator;
        private readonly ComboboxDataProvider _dataProvider;
        private readonly TableSchema _tableSchema;
        private readonly DebugLogger _debugLogger;
        private readonly OpenFileDialog _openFileDialog;
        private readonly SaveFileDialog _saveFileDialog;

        private Recipe _recipe;
        private LoopValidationResult _loopResult;
        private RecipeTimeAnalysis _timeResult;
        
        public BindingList<StepViewModel> ViewModels { get; }
        
        public IStatusManager StatusManager { get; }

        public event Action? OnUpdateStart;
        public event Action? OnUpdateEnd;
        
        public RecipeViewModel(
            RecipeEngine engine, RecipeLoopValidator loopValidator, RecipeTimeCalculator timeCalculator,
            ComboboxDataProvider dataProvider, IStatusManager statusManager, TableSchema tableSchema,
            DebugLogger debugLogger, OpenFileDialog openFileDialog, SaveFileDialog saveFileDialog)
        {
            _engine = engine;
            _loopValidator = loopValidator;
            _timeCalculator = timeCalculator;
            _dataProvider = dataProvider;
            StatusManager = statusManager;
            _tableSchema = tableSchema;
            _debugLogger = debugLogger;
            _openFileDialog = openFileDialog;
            _saveFileDialog = saveFileDialog;
            
            ViewModels = new BindingList<StepViewModel>();
            _recipe = _engine.CreateEmptyRecipe();
            
            UpdateRecipeStateAndViewModels(_recipe);
        }

        #region Public Commands (called from View)

        public void AddNewStep(int rowIndex)
        {
            _debugLogger.Log($"Adding new step at index {rowIndex}");
            var newRecipe = _engine.AddDefaultStep(_recipe, rowIndex);
            
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Add, rowIndex));
        }

        public void RemoveStep(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;

            _debugLogger.Log($"Removing step at index {rowIndex}");
            var newRecipe = _engine.RemoveStep(_recipe, rowIndex);
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Remove, rowIndex));
        }

        public void LoadRecipe()
        {
            if (_openFileDialog.ShowDialog() != DialogResult.OK) return;
        }

        public void SaveRecipe()
        {
            if (_saveFileDialog.ShowDialog() != DialogResult.OK) return;
        }

        #endregion
        
        private void    OnStepPropertyChanged(int rowIndex, ColumnKey key, object value)
        {
            Recipe newRecipe;
            RecipePropertyError? error;

            if (key == ColumnKey.Action && value is int newActionId)
            {
                newRecipe = _engine.ReplaceStepWithNewDefault(_recipe, rowIndex, newActionId);
                error = null;
            }
            else
            {
                (newRecipe, error) = _engine.UpdateStepProperty(_recipe, rowIndex, key, value);
            }

            if (error != null)
            {
                _debugLogger.Log($"Step property update failed. Row: {rowIndex}, Key: {key}, Value: '{value}'. Error: {error.Message}");
                StatusManager.WriteStatusMessage(error.Message, StatusMessage.Error);
                
                ViewModels.ResetItem(rowIndex);
                return;
            }

            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Update, rowIndex));
        }
        
        private void UpdateRecipeStateAndViewModels(Recipe newRecipe, StructuralChange? change = null)
        {
            OnUpdateStart?.Invoke();
            try
            {
                _recipe = newRecipe;
                _loopResult = _loopValidator.Validate(_recipe);
                _timeResult = _timeCalculator.Calculate(_recipe, _loopResult);

                if (!_loopResult.IsValid)
                {
                    StatusManager.WriteStatusMessage(_loopResult.ErrorMessage, StatusMessage.Error);
                }
                else
                {
                    StatusManager.ClearStatusMessage();
                }

                if (change == null || ViewModels.Count != _recipe.Steps.Count && change.Type != ChangeType.Add)
                {
                    ViewModels.RaiseListChangedEvents = false;
                    ViewModels.Clear();
                    foreach (var (step, index) in _recipe.Steps.Select((s, i) => (s, i)))
                    {
                        ViewModels.Add(CreateStepViewModel(step, index));
                    }
                    ViewModels.RaiseListChangedEvents = true;
                    ViewModels.ResetBindings();
                }
                else
                {
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
                OnUpdateEnd?.Invoke();
            }
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
            
            var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>() ?? 0;
            var availableTargets = _dataProvider.GetActionTargets(actionId);

            return new StepViewModel(
                step,
                _tableSchema,
                (key, val) => OnStepPropertyChanged(index, key, val),
                nestingLevel,
                startTime,
                availableTargets,
                _debugLogger
            );
        }

        #region Private Helper Classes

        private enum ChangeType { Add, Remove, Update }
        private record StructuralChange(ChangeType Type, int Index);

        #endregion
    }
}