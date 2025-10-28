using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

/// <summary>
/// Contains the complete results of a recipe time and loop structure analysis.
/// </summary>
/// <param name="TotalDuration">The total calculated execution time for the entire recipe.</param>
/// <param name="StepStartTimes">A dictionary mapping each step index to its absolute start time in a linear execution.</param>
/// <param name="LoopMetadataByStartIndex">A dictionary mapping a 'For' loop's start index to its full metadata.</param>
/// <param name="EnclosingLoopsMap">A dictionary mapping each step index to an ordered list of loops that contain it (outermost first).</param>
public sealed record LoopAnalysisResult(
    TimeSpan TotalDuration,
    IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
    IReadOnlyDictionary<int, LoopMetadata> LoopMetadataByStartIndex,
    IReadOnlyDictionary<int, IReadOnlyList<LoopMetadata>> EnclosingLoopsMap
);