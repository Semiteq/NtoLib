#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using InSAT.Library.Linq;
using NtoLib.Recipes.MbeTable.Composition;
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
        private readonly RecipeFileWriter _recipeFileWriter;
        private readonly RecipeFileReader _recipeFileReader;
        private readonly IRecipePlcSender _recipePlcSender;
        
        private Recipe _recipe;

        private LoopValidationResult _loopResult = new();
        private RecipeTimeAnalysis _timeResult = new();
        

        public event Action? OnUpdateStart;
        public event Action? OnUpdateEnd;
        public event Action<bool>? TogglePermissionToSendRecipe;
        public RecipeViewModel(
            IRecipeEngine recipeEngine,
            RecipeFileWriter recipeFileWriter,
            RecipeFileReader recipeFileReader,
            IRecipePlcSender recipePlcSender,
            RecipeLoopValidator recipeLoopValidator,
            RecipeTimeCalculator recipeTimeCalculator,
            IComboboxDataProvider comboboxDataProvider,
            IStatusManager statusManager,
            DebugLogger debugLogger)
        {
            _recipeEngine = recipeEngine ?? throw new ArgumentNullException(nameof(recipeEngine));
            _recipeFileWriter = recipeFileWriter ?? throw new ArgumentNullException(nameof(recipeFileWriter));
            _recipeFileReader = recipeFileReader ?? throw new ArgumentNullException(nameof(recipeFileReader));
            _recipePlcSender = recipePlcSender ?? throw new ArgumentNullException(nameof(recipePlcSender));
            _recipeLoopValidator = recipeLoopValidator ?? throw new ArgumentNullException(nameof(recipeLoopValidator));
            _recipeTimeCalculator = recipeTimeCalculator ?? throw new ArgumentNullException(nameof(recipeTimeCalculator));
            _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));;
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));;

            _recipe = _recipeEngine.CreateEmptyRecipe();

            UpdateRecipeStateAndViewModels(_recipe);
        }

        #region Public Commands

        public void AddNewStep(int rowIndex)
        {
            rowIndex = Math.Max(0, Math.Min(rowIndex, ViewModels.Count));

            _debugLogger.Log($"Adding new step at index {rowIndex}");
            var newRecipe = _recipeEngine.AddDefaultStep(_recipe, rowIndex);
            _debugLogger.Log($"Current step quantity {newRecipe.Steps.Count}");
            UpdateRecipeStateAndViewModels(newRecipe, new StructuralChange(ChangeType.Add, rowIndex));
        }

        public void RemoveStep(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ViewModels.Count) return;

            _debugLogger.Log($"Removing step at index {rowIndex}");
            var newRecipe = _recipeEngine.RemoveStep(_recipe, rowIndex);
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

        public async Task WriteRecipeToPlc()
        {
            _debugLogger.Log("Writing recipe to PLC");
            var plcSendResult = await _recipePlcSender.UploadAndVerifyAsync(_recipe);
            if (plcSendResult.IsFailed)
            {
                var errorMessages = string.Join(Environment.NewLine, plcSendResult.Errors.Select(e => e.Message));
                _statusManager.WriteStatusMessage(errorMessages, StatusMessage.Error);
            }
            _statusManager.WriteStatusMessage("Рецепт передан в контроллер без ошибок.", StatusMessage.Info);
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
                _loopResult = _recipeLoopValidator.Validate(_recipe);
                _timeResult = _recipeTimeCalculator.Calculate(_recipe, _loopResult);

                if (!_loopResult.IsValid)
                {
                    TogglePermissionToSendRecipe?.Invoke(false);
                    _statusManager.WriteStatusMessage(_loopResult.ErrorMessage, StatusMessage.Error);
                }
                else
                {
                    TogglePermissionToSendRecipe?.Invoke(true);
                    _statusManager.ClearStatusMessage();
                }

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

        #region Private Helper Classes

        private enum ChangeType { Add, Remove, Update }
        private record StructuralChange(ChangeType Type, int Index);

        #endregion
    }
}