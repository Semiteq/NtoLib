

using System;
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Attributes;

/// <summary>
/// Manages Recipe analysis results (loops, timing, structure validation).
/// Provides query API for accessing computed attributes.
/// </summary>
public interface IRecipeAttributesService
{
    /// <summary>
    /// Raised when validation state changes.
    /// </summary>
    event Action<bool>? ValidationStateChanged;

    /// <summary>
    /// Analyzes Recipe and updates internal state.
    /// </summary>
    Result UpdateAttributes(Recipe recipe);

    /// <summary>
    /// Gets loop nesting level for a specific step.
    /// </summary>
    Result<int> GetLoopNestingLevel(int stepIndex);

    /// <summary>
    /// Gets start time for a specific step.
    /// </summary>
    Result<TimeSpan> GetStepStartTime(int stepIndex);

    /// <summary>
    /// Gets all step start times.
    /// </summary>
    IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes();

    /// <summary>
    /// Gets total recipe duration.
    /// </summary>
    TimeSpan GetTotalDuration();

    /// <summary>
    /// Checks if the Recipe is valid (structure + loops + timing).
    /// </summary>
    bool IsValid();
}