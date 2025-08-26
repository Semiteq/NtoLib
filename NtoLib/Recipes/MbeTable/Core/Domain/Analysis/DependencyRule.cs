#nullable enable

using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis
{
    public record DependencyRule(
        IImmutableSet<ColumnIdentifier> TriggerKeys,
        ColumnIdentifier OutputKey,
        Delegate CalculationFunc
    );
}