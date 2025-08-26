#nullable enable

using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public record DependencyRulesMap
{
    private ImmutableList<DependencyRule> Map { get; init; }
    public DependencyRulesMap(StepCalculationLogic stepCalculationLogic)
    {
        Map = ImmutableList.Create(
            new DependencyRule(
                TriggerKeys: ImmutableHashSet.Create(WellKnownColumns.InitialValue, WellKnownColumns.Setpoint, WellKnownColumns.Speed),
                OutputKey: WellKnownColumns.StepDuration,
                CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)stepCalculationLogic
                    .CalculateDurationFromSpeed
            ),
            new DependencyRule(
                TriggerKeys: ImmutableHashSet.Create(WellKnownColumns.InitialValue, WellKnownColumns.Setpoint,
                    WellKnownColumns.StepDuration),
                OutputKey: WellKnownColumns.Speed,
                CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)stepCalculationLogic
                    .CalculateSpeedFromDuration
            )
        );
    }
    
    public ImmutableList<DependencyRule> GetMap => Map;
}