#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis
{
    /// <summary>
    /// Orchestrates the calculation of dependent properties for a step by delegating to configured calculation rules.
    /// </summary>
    public sealed class StepPropertyCalculator
    {
        private readonly IActionRepository _actionRepository;
        private readonly IReadOnlyDictionary<string, ICalculationRule> _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="StepPropertyCalculator"/> class.
        /// </summary>
        /// <param name="actionRepository">The repository to access action definitions.</param>
        /// <param name="rules">A dictionary of available calculation rule implementations, keyed by their unique name.</param>
        public StepPropertyCalculator(IActionRepository actionRepository, IEnumerable<ICalculationRule> rules)
        {
            _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
            _rules = rules?.ToDictionary(r => r.Name) ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Checks if a property change requires a dependency recalculation for a given step.
        /// </summary>
        /// <param name="step">The step being modified.</param>
        /// <returns>True if the step's action is configured with a calculation rule.</returns>
        public bool IsRecalculationRequired(Step step)
        {
            var actionId = step.Properties[WellKnownColumns.Action]!.GetValue<int>();
            var actionDef = _actionRepository.GetActionById(actionId);
            return actionDef.CalculationRule != null;
        }

        /// <summary>
        /// Calculates dependent properties by finding and applying the appropriate rule.
        /// </summary>
        /// <param name="currentStep">The current state of the step.</param>
        /// <param name="triggerKey">The key of the property that was changed.</param>
        /// <param name="newTriggerProperty">The new property value that triggered the calculation.</param>
        /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="Step"/> and an optional error.</returns>
        public Result<Step> CalculateDependencies(
            Step currentStep,
            ColumnIdentifier triggerKey,
            StepProperty newTriggerProperty)
        {
            var actionId = currentStep.Properties[WellKnownColumns.Action]!.GetValue<int>();
            var actionDef = _actionRepository.GetActionById(actionId);

            if (actionDef.CalculationRule is null)
            {
                // Should not happen if IsRecalculationRequired is checked first, but good practice.
                return Result.Ok(currentStep);
            }

            if (!_rules.TryGetValue(actionDef.CalculationRule.Name, out var rule))
            {
                var error = new CalculationError($"Calculation rule '{actionDef.CalculationRule.Name}' is defined in configuration but not implemented.");
                return Result.Fail(error);
            }

            // The mapping tells the rule which columns to use for its calculations.
            var mapping = actionDef.CalculationRule.Mapping;

            return rule.Apply(currentStep, triggerKey, newTriggerProperty, mapping);
        }
    }
}