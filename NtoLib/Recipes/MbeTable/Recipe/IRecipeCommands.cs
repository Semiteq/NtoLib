using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.Recipe
{
    // Этот интерфейс описывает команды для управления рецептом в целом
    public interface IRecipeCommands
    {
        // События, на которые подписывается UI
        event Action<Step, int> StepAdded;
        event Action<int> StepRemoved;
        event Action<int, string> StepPropertyChanged;

        // Методы, которые вызывает UI для изменения структуры рецепта
        bool TryAddNewStep(int rowIndex, out Step openStep, out string errorString);
        bool TryRemoveStep(int rowIndex, out string errorString);
        
        // Может понадобиться для загрузки/сохранения
        IReadOnlyList<Step> Steps { get; } 
    }
}