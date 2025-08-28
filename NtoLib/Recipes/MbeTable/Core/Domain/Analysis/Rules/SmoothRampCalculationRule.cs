#nullable enable

using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules;

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
    public (Step NewStep, RecipePropertyError? Error) Apply(
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
            return (currentStep, new CalculationError("Initial or Setpoint values are missing for calculation."));

        var initialValue = initialProp.GetValue<float>();
        var setpointValue = setpointProp.GetValue<float>();

        ColumnIdentifier outputKey;
        StepProperty targetProperty;
        (float? Value, CalculationError? Error) result;

        // Determine which value to calculate based on the trigger key
        if (triggerKey.Value == mapping.Rate) // Speed changed, so calculate duration
        {
            if (speedProp is null || durationProp is null) return (currentStep, null); // Not enough data
            outputKey = new ColumnIdentifier(mapping.Duration);
            targetProperty = durationProp;
            result = _logic.CalculateDurationFromSpeed(speedProp.GetValue<float>(), initialValue, setpointValue);
        }
        else // Duration, Initial or Setpoint changed, so calculate speed
        {
            if (speedProp is null || durationProp is null) return (currentStep, null); // Not enough data
            outputKey = new ColumnIdentifier(mapping.Rate);
            targetProperty = speedProp;
            result = _logic.CalculateSpeedFromDuration(durationProp.GetValue<float>(), initialValue, setpointValue);
        }

        if (result.Error != null) return (currentStep, result.Error);

        // Validate and apply the new calculated value
        var (success, finalNewProperty, validationError) = targetProperty.WithValue(result.Value!.Value);
        if (!success) return (currentStep, validationError);

        pendingChanges[outputKey] = finalNewProperty;

        var finalProperties = currentStep.Properties.SetItems(pendingChanges);
        return (currentStep with { Properties = finalProperties }, null);
    }
}