using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Loops;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;

/// <summary>
/// Immutable result of a full recipe analysis.
/// </summary>
public sealed record RecipeAnalysisSnapshot(
    Recipe Recipe,
    int StepCount,
    LoopTree LoopTree,
    IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
    TimeSpan TotalDuration,
    IReadOnlyList<IReason> Reasons,
    AnalysisFlags Flags,
    bool IsValid)
{
    public static RecipeAnalysisSnapshot Empty =>
        new(
            Recipe.Empty,
            0,
            LoopTree.Empty,
            ImmutableDictionary<int, TimeSpan>.Empty,
            TimeSpan.Zero,
            ImmutableArray<IReason>.Empty,
            AnalysisFlags.None,
            IsValid: false);
}