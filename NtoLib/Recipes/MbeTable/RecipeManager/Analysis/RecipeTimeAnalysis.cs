using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.RecipeManager.Analysis;

public record RecipeTimeAnalysis(
    TimeSpan TotalDuration,
    IReadOnlyDictionary<int, TimeSpan> StepStartTimes
);