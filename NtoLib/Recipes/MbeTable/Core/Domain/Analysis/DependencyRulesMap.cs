using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public record DependencyRulesMap
{
    private ImmutableList<DependencyRule> Map { get; init; }
    public DependencyRulesMap()
    {
        Map = ImmutableList.Create(
            new DependencyRule(
                TriggerKeys: ImmutableHashSet.Create(ColumnKey.InitialValue, ColumnKey.Setpoint, ColumnKey.Speed),
                OutputKey: ColumnKey.StepDuration,
                CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)StepCalculationLogic
                    .CalculateDurationFromSpeed
            ),
            new DependencyRule(
                TriggerKeys: ImmutableHashSet.Create(ColumnKey.InitialValue, ColumnKey.Setpoint,
                    ColumnKey.StepDuration),
                OutputKey: ColumnKey.Speed,
                CalculationFunc: (Func<float, float, float, (float?, CalculationError?)>)StepCalculationLogic
                    .CalculateSpeedFromDuration
            )
        );
    }
    
    public ImmutableList<DependencyRule> GetMap => Map;
}