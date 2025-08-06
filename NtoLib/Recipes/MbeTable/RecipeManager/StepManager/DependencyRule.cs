#nullable enable

using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.RecipeManager.StepManager
{
    public record DependencyRule(
        IImmutableSet<ColumnKey> TriggerKeys,
        ColumnKey OutputKey,
        Delegate CalculationFunc
    );
}