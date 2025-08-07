#nullable enable

using System;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis
{
    public record DependencyRule(
        IImmutableSet<ColumnKey> TriggerKeys,
        ColumnKey OutputKey,
        Delegate CalculationFunc
    );
}