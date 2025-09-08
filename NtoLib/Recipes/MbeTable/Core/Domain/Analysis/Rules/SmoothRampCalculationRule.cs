#nullable enable

using System;
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules
{
    /// <summary>
    /// Implements the calculation logic for a smooth temperature or power ramp.
    /// It calculates duration based on speed, or speed based on duration.
    /// </summary>
    public sealed class SmoothRampCalculationRule : ICalculationRule
    {
        private readonly StepCalculationLogic _logic;

        public SmoothRampCalculationRule(StepCalculationLogic logic)
        {
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        }

        /// <inheritdoc />
        public string Name => "SmoothRampCalculation";

        /// <inheritdoc />
        public Result<Step> Apply(
            Step currentStep,
            ColumnIdentifier triggerKey,
            StepProperty newTriggerProperty,
            CalculationRuleMapping mapping)
        {
            var pendingChanges = new Dictionary<ColumnIdentifier, StepProperty> { [triggerKey] = newTriggerProperty };
            var context = currentStep.Properties.SetItems(pendingChanges);

            // Extract values using the mapping from the configuration
            context.TryGetValue(new ColumnIdentifier(mapping.Initial), out var initialProp);
            context.TryGetValue(new ColumnIdentifier(mapping.Final), out var setpointProp);
            context.TryGetValue(new ColumnIdentifier(mapping.Duration), out var durationProp);
            context.TryGetValue(new ColumnIdentifier(mapping.Rate), out var speedProp);

            if (initialProp is null || setpointProp is null)
                return Result.Fail(new CalculationError("Initial or Setpoint values are missing for calculation."));

            var initialValue = initialProp.GetValue<float>();
            var setpointValue = setpointProp.GetValue<float>();

            ColumnIdentifier outputKey;
            StepProperty targetProperty;
            Result<float> calculationResult;

            // Determine which value to calculate based on the trigger key
            if (triggerKey.Value == mapping.Rate) // Speed changed, so calculate duration
            {
                if (speedProp is null || durationProp is null) return Result.Ok(currentStep); // Not enough data, but not an error
                outputKey = new ColumnIdentifier(mapping.Duration);
                targetProperty = durationProp;
                calculationResult = _logic.CalculateDurationFromSpeed(speedProp.GetValue<float>(), initialValue, setpointValue);
            }
            else // Duration, Initial or Setpoint changed, so calculate speed
            {
                if (speedProp is null || durationProp is null) return Result.Ok(currentStep); // Not enough data, but not an error
                outputKey = new ColumnIdentifier(mapping.Rate);
                targetProperty = speedProp;
                calculationResult = _logic.CalculateSpeedFromDuration(durationProp.GetValue<float>(), initialValue, setpointValue);
            }

            if (calculationResult.IsFailed)
            {
                return Result.Fail(calculationResult.Errors);
            }

            // Validate and apply the new calculated value
            var updateResult = targetProperty.WithValue(calculationResult.Value);
            if (updateResult.IsFailed)
            {
                return Result.Fail(updateResult.Errors);
            }
            
            pendingChanges[outputKey] = updateResult.Value;

            var finalProperties = currentStep.Properties.SetItems(pendingChanges);
            var newStep = currentStep with { Properties = finalProperties };
            
            return Result.Ok(newStep);
        }
    }
}