#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis
{
    /// <summary>
    /// Calculates dependent properties for a single step based on a set of dependency rules.
    /// This class embodies the Strategy pattern, where each rule is a strategy for calculation.
    /// </summary>
    public class StepPropertyCalculator
    {
        private readonly IImmutableList<DependencyRule> _rules;
        private readonly IImmutableSet<ColumnKey> _linkedColumns;

        public StepPropertyCalculator(IImmutableList<DependencyRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _linkedColumns = _rules.SelectMany(r => r.TriggerKeys).Union(_rules.Select(r => r.OutputKey))
                .ToImmutableHashSet();
        }

        public bool IsRecalculationRequired(Step step, ColumnKey changedKey, IImmutableSet<int> smoothActionIds)
        {
            var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>();
            return actionId.HasValue && smoothActionIds.Contains(actionId.Value) &&
                   _linkedColumns.Contains(changedKey);
        }

        public (Step NewStep, RecipePropertyError? Error) CalculateDependencies(
            Step currentStep,
            ColumnKey triggerKey,
            StepProperty newTriggerProperty)
        {
            var pendingChanges = new Dictionary<ColumnKey, StepProperty> { [triggerKey] = newTriggerProperty };
            var affectedRules = _rules.Where(rule => rule.TriggerKeys.Contains(triggerKey));

            foreach (var rule in affectedRules)
            {
                var context = CreateCalculationContext(currentStep.Properties, pendingChanges);

                // Todo: This part is still complex and could be improved by refactoring DependencyRule
                // to encapsulate the function signature and parameter mapping.
                // For now, we keep the existing logic but contained within this class.
                context.TryGetValue(ColumnKey.InitialValue, out var initialValue);
                context.TryGetValue(ColumnKey.Setpoint, out var setpoint);
                context.TryGetValue(ColumnKey.StepDuration, out var duration);
                context.TryGetValue(ColumnKey.Speed, out var speed);

                (float? calculatedValue, CalculationError? calcError) result;
                if (rule.OutputKey == ColumnKey.StepDuration && speed is not null && initialValue is not null &&
                    setpoint is not null)
                {
                    var func = (Func<float, float, float, (float?, CalculationError?)>)rule.CalculationFunc;
                    result = func(speed.GetValue<float>(), initialValue.GetValue<float>(), setpoint.GetValue<float>());
                }
                else if (rule.OutputKey == ColumnKey.Speed && duration is not null && initialValue is not null &&
                         setpoint is not null)
                {
                    var func = (Func<float, float, float, (float?, CalculationError?)>)rule.CalculationFunc;
                    result = func(duration.GetValue<float>(), initialValue.GetValue<float>(), setpoint.GetValue<float>());
                }
                else
                {
                    continue;
                }

                if (result.calcError != null)
                {
                    return (currentStep, result.calcError);
                }

                var targetProperty = context[rule.OutputKey];
                if (targetProperty == null) continue;

                var (success, finalNewProperty, validationError) =
                    targetProperty.WithValue(result.calculatedValue!.Value);
                if (!success)
                {
                    return (currentStep, validationError);
                }

                pendingChanges[rule.OutputKey] = finalNewProperty;
            }

            var finalProperties = currentStep.Properties.SetItems(pendingChanges);
            return (currentStep with { Properties = finalProperties }, null);
        }

        private IReadOnlyDictionary<ColumnKey, StepProperty?> CreateCalculationContext(
            IImmutableDictionary<ColumnKey, StepProperty?> currentProperties,
            IReadOnlyDictionary<ColumnKey, StepProperty> pendingChanges)
        {
            return currentProperties.SetItems(pendingChanges);
        }
    }
}

