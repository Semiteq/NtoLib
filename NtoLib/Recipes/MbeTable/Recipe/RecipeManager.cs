using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe;

public class RecipeManager : IStepUpdater
{
    /// <summary>
    /// Encapsulates the core business logic and rules for managing a recipe.
    /// It is the single source of truth for the recipe data (the Model)
    /// and is completely independent of any UI framework. This strict separation allows the core logic to be reused,
    /// tested, and maintained without considering how it will be displayed.
    /// </summary>

    public event Action<Step, int> StepAdded;
    public event Action<int> StepRemoved;
    public event Action<int, ColumnKey> StepPropertyChanged;
    
    private readonly TableSchema _tableSchema;
    private readonly StepFactory _stepFactory;

    private readonly List<Step> _recipe;

    public RecipeManager(TableSchema schema, StepFactory stepFactory)
    {
        _tableSchema = schema ?? throw new ArgumentNullException(nameof(schema));
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        
        _recipe = new List<Step>();
    }

    public IReadOnlyList<Step> Steps => _recipe.AsReadOnly();

    public bool TryAddDefaultStep(int rowIndex, out Step openStep, out string errorString)
    {
        // A new step is always an open step
        openStep = null;
        errorString = null;

        // If added at the end, rowIndex should be equal to Count
        rowIndex = Math.Max(0, Math.Min(rowIndex, _recipe.Count));

        openStep = _stepFactory.CreateOpenStep(1); // todo: get minimal id from shutters

        _recipe.Insert(rowIndex, openStep);

        StepAdded?.Invoke(openStep, rowIndex);

        return true;
    }

    public bool TrySetStepPropertyByObject(int rowIndex, ColumnKey columnKey, object value, out string errorString)
    {
        var result = _recipe[rowIndex].TryUpdatePropertyAndDependencies(columnKey, value, out var affectedKeys,  out errorString);
        
        if (result)
        {
            foreach (var affectedKey in affectedKeys)
            {
                StepPropertyChanged?.Invoke(rowIndex, affectedKey);
            }
        }
        
        return result;
    }

    public bool TryRemoveStep(int rowIndex, out string errorString)
    {
        if (!ValidateRow(rowIndex, out errorString))
            return false;

        _recipe.RemoveAt(rowIndex);

        StepRemoved?.Invoke(rowIndex);

        return true;
    }

    private bool ValidateRow(int rowIndex, out string errorString)
    {
        errorString = string.Empty;
        if (rowIndex < 0 || rowIndex >= _recipe.Count)
        {
            errorString = @"Row index is out of range";
            return false;
        }

        return true;
    }

}