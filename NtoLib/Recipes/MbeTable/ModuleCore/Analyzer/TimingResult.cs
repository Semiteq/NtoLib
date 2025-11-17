using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.ModuleCore.Loops;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Timing calculation outcome.
/// </summary>
public sealed record TimingResult(
    IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
    TimeSpan TotalDuration,
    IReadOnlyList<LoopNode> UpdatedNodes);