using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

public record RecipeTimeAnalysis(
    TimeSpan TotalDuration,
    IReadOnlyDictionary<int, TimeSpan> StepStartTimes
);