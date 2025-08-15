#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using InSAT.Library.Linq;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.RecipeFile;
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

        private readonly RecipeEngine _engine;
        private readonly RecipeLoopValidator _loopValidator;
        private readonly RecipeTimeCalculator _timeCalculator;
        private readonly ComboboxDataProvider _dataProvider;
        private readonly DebugLogger _debugLogger;
        private readonly IStatusManager _statusManager;
        private readonly RecipeFileWriter _recipeFileWriter;
        private readonly RecipeFileReader _recipeFileReader;

        private Recipe _recipe;

        private LoopValidationResult _loopResult = new();
        private RecipeTimeAnalysis _timeResult = new();

        public event Action? OnUpdateStart;
        public event Action? OnUpdateEnd;

        public RecipeViewModel(
            RecipeEngine engine,
            RecipeFileWriter recipeFileWriter,
            RecipeFileReader recipeFileReader,
            RecipeLoopValidator loopValidator,
            RecipeTimeCalculator timeCalculator,
            ComboboxDataProvider dataProvider,
            IStatusManager statusManager,
            DebugLogger debugLogger)
        {
            _engine = engine;
            _recipeFileWriter = recipeFileWriter;
            _recipeFileReader = recipeFileReader;
            _loopValidator = loopValidator;
            _timeCalculator = timeCalculator;
            _dataProvider = dataProvider;
            _statusManager = statusManager;
            _debugLogger = debugLogger;

            _recipe = _engine.CreateEmptyRecipe();

            UpdateRecipeStateAndViewModels(_recipe);
        }

        #region Public Commands

        public void AddNewStep(int rowIndex)
        {
            rowIndex = Math.Max(0, Math.Min(rowIndex, ViewModels.Count));

            _debugLogger.Log($"Adding new step at index {rowIndex}");
            var newRecipe = _engine.AddDefaultStep(_recipe, rowIndex);
            _debugLogger.Log($"Current step quantity {newRecipe.Steps.Count}");
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Add, rowIndex));
        }

        public void RemoveStep(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;

            _debugLogger.Log($"Removing step at index {rowIndex}");
            var newRecipe = _engine.RemoveStep(_recipe, rowIndex);
            _debugLogger.Log($"Current step quantity {newRecipe.Steps.Count}");
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Remove, rowIndex));
        }

        public void LoadRecipe(string filePath)
        {
            _debugLogger.Log($"Loading recipe from file: {filePath}");

            var (recipe, errors) = _recipeFileReader.Read(filePath);

            if (errors.IsEmpty() && recipe != null)
            {
                var newRecipe = recipe;
                _debugLogger.Log($"Successfully loaded file");
                UpdateRecipeStateAndViewModels(newRecipe);
                _statusManager.WriteStatusMessage($"Файл загружен: {filePath}", StatusMessage.Info);
            }
            else
            {
                _debugLogger.Log($"Failed to load recipe from file: {filePath}. Errors: {errors}");

                var multilineErrors = MultilineErrors(errors);
                
                _statusManager.WriteStatusMessage($"Найдены ошибки при чтении ({errors.Count}): \r\n{multilineErrors}", StatusMessage.Error);
            }
        }

        public void SaveRecipe(string filePath)
        {
            _debugLogger.Log($"Saving recipe to file: {filePath}");

            var errors = _recipeFileWriter.Write(_recipe, filePath);
            
            if (!errors.IsEmpty())
            {
                _debugLogger.Log($"Failed to save recipe to file: {filePath}. Errors: {errors}");
                
                var multilineErrors = MultilineErrors(errors);
                
                _statusManager.WriteStatusMessage($"Найдены ошибки при сохранении ({errors.Count}): \r\n{multilineErrors}", StatusMessage.Error);
            }
            
            _debugLogger.Log($"Successfully saved file");
            _statusManager.WriteStatusMessage($"Файл сохранен: {filePath}", StatusMessage.Info);
        }

        #endregion

        #region Private Properties
        /// <summary>
        /// Handles updates to a step's property within the recipe, ensuring the recipe remains valid
        /// and updating the related ViewModels and UI accordingly.
        /// If the update encounters an error, it resets the affected item and logs the issue.
        /// </summary>
        /// <param name="rowIndex">The index of the step being updated.</param>
        /// <param name="key">The property key of the step being updated (e.g., Action, Setpoint).</param>
        /// <param name="value">The new value to be assigned to the specified property key.</param>
        private void OnStepPropertyChanged(int rowIndex, ColumnKey key, object value)
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
                _statusManager.WriteStatusMessage(error.Message, StatusMessage.Error);

                ViewModels.ResetItem(rowIndex);
                return;
            }
            _debugLogger.Log($"Step property updated. Row: {rowIndex}, Key: {key}, Value: '{value}'");
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Update, rowIndex));
        }

        /// <summary>
        /// Updates the current recipe state and synchronizes the associated ViewModels.
        /// This involves updating the internal recipe data, validating the loop,
        /// recalculating the recipe time, and managing UI updates based on any structural changes in the recipe.
        /// </summary>
        /// <param name="newRecipe">The new recipe instance to be updated in the ViewModels.</param>
        /// <param name="change">An optional structural change object describing changes to the recipe (e.g., addition or removal of a step).</param>
        private void UpdateRecipeStateAndViewModels(Recipe newRecipe, StructuralChange? change = null)
        {
            OnUpdateStart?.Invoke();

            try
            {
                _recipe = newRecipe;
                _loopResult = _loopValidator.Validate(_recipe);
                _timeResult = _timeCalculator.Calculate(_recipe, _loopResult);

                if (!_loopResult.IsValid)
                    _statusManager.WriteStatusMessage(_loopResult.ErrorMessage, StatusMessage.Error);
                else
                    _statusManager.ClearStatusMessage();

                if (change == null || ViewModels.Count != _recipe.Steps.Count && change.Type != ChangeType.Add)
                {
                    // Repopulate ViewModel if init or error
                    ViewModels.RaiseListChangedEvents = false;
                    ViewModels.Clear();
                    foreach (var (step, index) in _recipe.Steps.Select((s, i) => (s, i)))
                    {
                        ViewModels.Add(CreateStepViewModel(step, index));
                    }
                    ViewModels.RaiseListChangedEvents = true;
                    ViewModels.ResetBindings();

                    _debugLogger.Log("ViewModel was repopulated.");
                }
                else
                {
                    // Partial update
                    // ViewModel handles update by itself 
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
        }

        /// <summary>
        /// Updates the ViewModels for all steps in the recipe starting from a specified index.
        /// This ensures that the ViewModels remain synchronized with the underlying recipe
        /// whenever structural changes, such as addition, removal, or updates to steps, occur.
        /// First disables UI updates during batch operation, then enables it.
        /// </summary>
        /// <param name="startIndex">The zero-based index of the first step to update in the ViewModels.</param>
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

        /// <summary>
        /// Creates a new instance of the <see cref="StepViewModel"/> class for a given recipe step.
        /// This involves initializing the step ViewModel with its associated properties, metadata,
        /// and dependencies, such as nesting level, start time, and available targets, based on its
        /// associated action and schema configurations.
        /// </summary>
        /// <param name="step">The step entity that needs to be converted into a ViewModel.</param>
        /// <param name="index">The index of the step within the recipe's step collection.</param>
        /// <returns>A new <see cref="StepViewModel"/> instance representing the provided step.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the step does not have a valid action ID.</exception>
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

            var availableTargets = _dataProvider.GetActionTargets(actionId.Value);

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

        #region Private Helper Classes

        private enum ChangeType { Add, Remove, Update }
        private record StructuralChange(ChangeType Type, int Index);

        #endregion
    }
}