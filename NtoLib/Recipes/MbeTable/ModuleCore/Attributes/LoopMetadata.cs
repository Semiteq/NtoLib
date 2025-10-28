using System;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

/// <summary>
/// Represents immutable, static metadata about a single loop structure within a recipe.
/// This data is pre-calculated by the RecipeTimeCalculator.
/// </summary>
/// <param name="StartIndex">The zero-based index of the 'For' step.</param>
/// <param name="EndIndex">The zero-based index of the 'EndFor' step.</param>
/// <param name="NestingDepth">The nesting depth level of this loop (1-based, corresponds to PLC counters ForLevel1-3).</param>
/// <param name="IterationDuration">The pre-calculated duration of a single loop iteration.</param>
/// <param name="IterationCount">The number of iterations configured for this loop in the recipe task.</param>
public sealed record LoopMetadata(
    int StartIndex,
    int EndIndex,
    int NestingDepth,
    TimeSpan IterationDuration,
    int IterationCount
);