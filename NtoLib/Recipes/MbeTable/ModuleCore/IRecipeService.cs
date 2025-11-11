using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Attributes;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

// Core domain service for recipe state, mutations, and analysis.
public interface IRecipeService
{
    // Fires when the validation state changes; arg = new state.
    event Action<bool>? ValidationStateChanged;

    Recipe CurrentRecipe { get; }

    int StepCount { get; }

    Result<TimeSpan> GetStepStartTime(int stepIndex);

    TimeSpan GetTotalDuration();

    // Key = step index.
    IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes();

    bool IsValid();

    Result<int> GetLoopNestingLevel(int stepIndex);

    // Innermost → outermost for the given step.
    IReadOnlyList<LoopMetadata> GetEnclosingLoops(int stepIndex);

    // Replaces current recipe and re-analyzes.
    Result SetRecipeAndUpdateAttributes(Recipe recipe);

    Result AddStep(int rowIndex);

    Result RemoveStep(int rowIndex);

    Result UpdateStepProperty(int rowIndex, ColumnIdentifier columnIdentifier, object value);

    Result ReplaceStepAction(int rowIndex, short newActionId);
}