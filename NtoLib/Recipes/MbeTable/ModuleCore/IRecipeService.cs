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
    Recipe CurrentRecipe { get; }
    int StepCount { get; }
    Result<TimeSpan> GetStepStartTime(int stepIndex);
    TimeSpan GetTotalDuration();
    IReadOnlyDictionary<int, TimeSpan> GetAllStepStartTimes();
    bool IsValid();
    IReadOnlyList<LoopMetadata> GetEnclosingLoops(int stepIndex);
    Result<ValidationSnapshot> SetRecipeAndUpdateAttributes(Recipe recipe);
    Result<ValidationSnapshot> AddStep(int rowIndex);
    Result<ValidationSnapshot> RemoveStep(int rowIndex);
    Result<ValidationSnapshot> UpdateStepProperty(int rowIndex, ColumnIdentifier columnIdentifier, object value);
    Result<ValidationSnapshot> ReplaceStepAction(int rowIndex, short newActionId);
}