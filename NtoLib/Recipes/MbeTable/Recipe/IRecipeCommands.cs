using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe
{
    public interface IRecipeCommands
    {
        event Action<Step, int> StepAdded;
        event Action<int> StepRemoved;
        event Action<int, ColumnKey> StepPropertyChanged;

        bool TryAddNewStep(int rowIndex, out Step openStep, out string errorString);
        bool TryRemoveStep(int rowIndex, out string errorString);
        IReadOnlyList<Step> Steps { get; } 
    }
}