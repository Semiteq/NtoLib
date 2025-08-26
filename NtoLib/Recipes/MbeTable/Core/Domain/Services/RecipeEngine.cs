#nullable enable

using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services
{
    /// <summary>
    /// A stateless service that encapsulates all business logic for recipe manipulation.
    /// It operates on immutable Recipe and Step records, taking a state and an action,
    /// and returning a new state. This is the "functional core" of the recipe management system.
    /// </summary>
    public sealed class RecipeEngine : IRecipeEngine
    {
        private readonly ActionManager _actionManager;
        private readonly IStepFactory _stepFactory;
        private readonly IActionTargetProvider _actionTargetProvider;
        private readonly StepPropertyCalculator _stepPropertyCalculator;
        private readonly IImmutableSet<int> _smoothActionIds;
        private readonly DebugLogger _debugLogger;

        public RecipeEngine(ActionManager actionManager,
            IStepFactory stepFactory,
            IActionTargetProvider actionTargetProvider,
            StepPropertyCalculator stepPropertyCalculator,
            DebugLogger debugLogger)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
            _actionTargetProvider =
                actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
            _stepPropertyCalculator = stepPropertyCalculator ??
                                      throw new ArgumentNullException(nameof(stepPropertyCalculator));

            _smoothActionIds = new[] { _actionManager.PowerSmooth.Id, _actionManager.TemperatureSmooth.Id }
                .ToImmutableHashSet();
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        }

        public Recipe CreateEmptyRecipe() => new(ImmutableList<Step>.Empty);

        public Recipe AddDefaultStep(Recipe currentRecipe, int rowIndex)
        {
            _debugLogger.Log("Step added");

            var minimalShutterId = _actionTargetProvider.GetMinimalShutterId();
            var newStep = _stepFactory.ForAction(_actionManager.Open.Id).WithOptionalTarget(minimalShutterId).Build();

            return new Recipe(Steps: currentRecipe.Steps.Insert(rowIndex, newStep));
        }

        public Recipe RemoveStep(Recipe currentRecipe, int rowIndex)
        {
            _debugLogger.Log("Step removed");
            return new Recipe(Steps: currentRecipe.Steps.RemoveAt(rowIndex));
        }

        /// <summary>
        /// Called when the user selects a new action in the combobox.
        /// </summary>
        public Recipe ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId)
        {
            if (rowIndex < 0 || rowIndex >= currentRecipe.Steps.Count) return currentRecipe;

            var actionType = _actionManager.GetActionTypeById(newActionId);
            var targetId = GetDefaultTargetForActionType(actionType);

            var newDefaultStep = _stepFactory.ForAction(newActionId).WithOptionalTarget(targetId).Build();

            var newSteps = currentRecipe.Steps.SetItem(rowIndex, newDefaultStep);
            return new Recipe(Steps: newSteps);
        }

        public (Recipe NewRecipe, RecipePropertyError? Error) UpdateStepProperty(
            Recipe currentRecipe, int rowIndex, ColumnIdentifier columnKey, object value)
        {
            if (rowIndex < 0 || rowIndex >= currentRecipe.Steps.Count)
                return (currentRecipe, new ValidationError("Row index is out of range."));

            var stepToUpdate = currentRecipe.Steps[rowIndex];
            var (newStep, error) = ApplyUpdateToStep(stepToUpdate, columnKey, value);

            if (error != null) return (currentRecipe, error);

            var newSteps = currentRecipe.Steps.SetItem(rowIndex, newStep);
            return (new Recipe(Steps: newSteps), null);
        }

        private int GetDefaultTargetForActionType(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Heater => _actionTargetProvider.GetMinimalHeaterId(),
                ActionType.Shutter => _actionTargetProvider.GetMinimalShutterId(),
                ActionType.NitrogenSource => _actionTargetProvider.GetMinimalNitrogenSourceId(),
                _ => 0
            };
        }

        private (Step NewStep, RecipePropertyError? Error) ApplyUpdateToStep(
            Step currentStep, ColumnIdentifier columnKey, object value)
        {
            if (!currentStep.Properties.TryGetValue(columnKey, out var propertyToUpdate) || propertyToUpdate == null)
            {
                _debugLogger.Log($"Error: Attempted to update non-existent property '{columnKey.Value}' on a step.", "ApplyUpdateToStep");
                return (currentStep, new ValidationError($"Property {columnKey.Value} is not available."));
            }

            var (success, newProperty, error) = propertyToUpdate.WithValue(value);
            if (!success)
            {
                _debugLogger.Log($"Property validation failed for '{columnKey.Value}' with value '{value}'. Reason: {error?.Message}", "ApplyUpdateToStep");
                return (currentStep, error);
            }

            if (!_stepPropertyCalculator.IsRecalculationRequired(currentStep, columnKey, _smoothActionIds))
            {
                var newProperties = currentStep.Properties.SetItem(columnKey, newProperty);
                return (currentStep with { Properties = newProperties }, null);
            }

            return _stepPropertyCalculator.CalculateDependencies(currentStep, columnKey, newProperty);
        }
    }
}