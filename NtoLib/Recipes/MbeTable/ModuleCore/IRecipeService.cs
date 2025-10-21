using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

/// <summary>
/// Core domain service managing Recipe state and coordinating mutations and analysis.
/// </summary>
public interface IRecipeService
{
    /// <summary>
    /// Raised when validation state changes.
    /// </summary>
    event Action<bool>? ValidationStateChanged;

    /// <summary>
    /// Gets the current Recipe.
    /// </summary>
    Recipe GetCurrentRecipe();

    /// <summary>
    /// Gets the start time for a specific step.
    /// </summary>
    Result<TimeSpan> GetStepStartTime(int stepIndex);

    /// <summary>
    /// Gets the total duration of the current Recipe.
    /// </summary>
    TimeSpan GetTotalDuration();

    /// <summary>
    /// Gets all step start times.
    /// </summary>
    IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes();

    /// <summary>
    /// Checks if the current Recipe is valid.
    /// </summary>
    bool IsValid();

    /// <summary>
    /// Gets loop nesting level for a specific step.
    /// </summary>
    Result<int> GetLoopNestingLevel(int stepIndex);

    /// <summary>
    /// Replaces current Recipe with provided one, validates and analyzes it.
    /// </summary>
    Result SetRecipe(Recipe recipe);

    /// <summary>
    /// Adds a default step at the specified row index.
    /// </summary>
    Result AddStep(int rowIndex);

    /// <summary>
    /// Removes step at the specified row index.
    /// </summary>
    Result RemoveStep(int rowIndex);

    /// <summary>
    /// Updates a property of a specific step.
    /// </summary>
    Result UpdateStepProperty(int rowIndex, ColumnIdentifier key, object value);

    /// <summary>
    /// Replaces step action at the specified row index.
    /// </summary>
    Result ReplaceStepAction(int rowIndex, short newActionId);
}