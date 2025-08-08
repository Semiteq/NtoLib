using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public record RecipeTimeAnalysis(TimeSpan TotalDuration, IReadOnlyDictionary<int, TimeSpan> StepStartTimes)
{
    // Non-nullable state
    public RecipeTimeAnalysis() : this(TimeSpan.Zero, new Dictionary<int, TimeSpan>())
    {
    }
}