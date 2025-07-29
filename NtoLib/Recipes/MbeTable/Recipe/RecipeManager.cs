using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe;

public class RecipeManager : IStepUpdater, IRecipeCommands
{
    /// <summary>
    /// Encapsulates the core business logic and rules for managing a recipe.
    /// It is the single source of truth for the recipe data (the Model)
    /// and is completely independent of any UI framework. This strict separation allows the core logic to be reused,
    /// tested, and maintained without considering how it will be displayed.
    /// </summary>

    public event Action<Step, int> StepAdded;
    public event Action<int> StepRemoved;
    public event Action<int, string> StepPropertyChanged;

    private readonly List<Step> _recipe = new();
    private readonly StepFactory _stepFactory;
    private readonly PropertyDependencyCalc _propertyDependencyCalc;

    private readonly TableSchema _tableSchema;

    public RecipeManager(TableSchema schema, ActionManager actionManager)
    {
        _tableSchema = schema ?? throw new ArgumentNullException(nameof(schema));
        _propertyDependencyCalc = new PropertyDependencyCalc(actionManager, _tableSchema);
        _stepFactory = new StepFactory(actionManager, schema);
    }

    public IReadOnlyList<Step> Steps => _recipe.AsReadOnly();
    public int StepCount => _recipe.Count;

    public bool TryAddNewStep(int rowIndex, out Step openStep, out string errorString)
    {
        // A new step is always an open step
        openStep = null;
        errorString = null;

        // If added at the end, rowIndex should be equal to Count
        rowIndex = Math.Max(0, Math.Min(rowIndex, _recipe.Count));

        if (!_stepFactory.TryCreateOpenStep(out openStep, out errorString, 1))
        {
            return false;
        }

        _recipe.Insert(rowIndex, openStep);

        StepAdded?.Invoke(openStep, rowIndex);

        return true;
    }

    public bool TryRemoveStep(int rowIndex, out string errorString)
    {
        if (!ValidateRow(rowIndex, out errorString))
            return false;

        _recipe.RemoveAt(rowIndex);

        StepRemoved?.Invoke(rowIndex);

        return true;
    }

    public bool TryGetStep(int rowIndex, out Step step, out string errorString)
    {
        if (!ValidateRow(rowIndex, out errorString))
        {
            step = null;
            return false;
        }

        step = _recipe[rowIndex];
        return true;
    }

    public bool TrySetStepProperty(int rowIndex, int columnIndex, PropertyValue value,
        out int[] dependencyIndexes, out string errorString)
    {
        dependencyIndexes = Array.Empty<int>();

        if (!ValidateRow(rowIndex, out errorString))
            return false;

        var step = _recipe[rowIndex];
        var columnKey = _tableSchema.GetColumnKeyByIndex(columnIndex);

        return step.TryChangePropertyValue(columnKey, value, out errorString)
               && _propertyDependencyCalc.TryRecalculate(step, columnIndex, out dependencyIndexes, out errorString);
    }


    public bool TrySetStepPropertyByObject(int rowIndex, ColumnKey columnKey, object value,
        out string errorString)
    {
        if (!ValidateRow(rowIndex, out errorString))
            return false;

        var step = _recipe[rowIndex];

        int columnIndex = _tableSchema.GetIndexByColumnKey(columnKey);

        if (!step.TryChangePropertyValue(columnKey, value, out errorString))
            return false;

        if (!_propertyDependencyCalc.TryRecalculate(step, columnIndex, out var dependencyIndexes, out errorString))
            return false;

        // Binding name to ColumnKey; todo: improve this
        // Achtung! This mapping is name-based and unreliable in case property names change.
        string propertyName = Enum.GetName(typeof(ColumnKey), columnKey);

        StepPropertyChanged?.Invoke(rowIndex, propertyName);

        foreach (var depIndex in dependencyIndexes)
        {
            var depKey = _tableSchema.GetColumnKeyByIndex(depIndex);
            string depPropertyName = Enum.GetName(typeof(ColumnKey), depKey); // binding name to ColumnKey
            StepPropertyChanged?.Invoke(rowIndex, depPropertyName);
        }

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