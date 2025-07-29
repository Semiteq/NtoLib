using System;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

public class PropertyDependencyCalc
{
    private readonly ActionManager _actionManager;
    private readonly TableSchema _schema;

    public PropertyDependencyCalc(ActionManager actionManager, TableSchema schema)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }
    
    public bool TryRecalculate(Step step, int columnIndex, out int[] indexesToUpdate, out string errorString)
    {
        indexesToUpdate = Array.Empty<int>();
        errorString = string.Empty;
        
        if (!step.TryGetPropertyWrapper(ColumnKey.Action, out var actionWrapper) || !IsSmoothAction(actionWrapper.PropertyValue.AsInt))
            return true;
        
        var changedColumnKey = _schema.GetColumnKeyByIndex(columnIndex);
        var dependentColumns = GetDependentColumns(changedColumnKey);

        foreach (var key in dependentColumns)
        {
            if (!RecalculatePropertyDependencies(step, key, out errorString))
                return false;
        }
        
        indexesToUpdate = dependentColumns
            .Select(key => _schema.GetIndexByColumnKey(key))
            .ToArray();

        return indexesToUpdate.Length > 0;
    }

    public bool RecalculatePropertyDependencies(Step step, ColumnKey changedProperty, out string errorString)
    {
        errorString = null;

        return changedProperty switch
        {
            ColumnKey.Speed or ColumnKey.InitialValue or ColumnKey.Setpoint => TryRecalculateDuration(step, out errorString),
            ColumnKey.Duration => TryRecalculateSpeed(step, out errorString),
            _ => true
        };
    }
    
    private bool IsSmoothAction(int actionId) =>
        _actionManager.GetActionEntryById(actionId, out var actionEntry, out _) &&
        (actionEntry == _actionManager.PowerSmooth || actionEntry == _actionManager.TemperatureSmooth);

    private static ColumnKey[] GetDependentColumns(ColumnKey changedColumn) =>
        changedColumn switch
        {
            ColumnKey.Speed or ColumnKey.InitialValue or ColumnKey.Setpoint => new[] { ColumnKey.Duration },
            ColumnKey.Duration => new[] { ColumnKey.Speed },
            _ => Array.Empty<ColumnKey>()
        };

    private bool TryRecalculateDuration(Step step, out string errorString) =>
        TryGetValuesForCalculation(step, out var speed, out var setpoint, out var initial, out errorString) &&
        TryCalculateAndSetDuration(step, speed, setpoint, initial, out errorString);

    private bool TryRecalculateSpeed(Step step, out string errorString) =>
        TryGetValuesForCalculation(step, out var duration, out var setpoint, out var initial, out errorString) &&
        TryCalculateAndSetSpeed(step, duration, setpoint, initial, out errorString);

    private bool TryGetValuesForCalculation(Step step, out float value1, out float value2, out float value3, out string errorString)
    {
        errorString = null;
        value1 = value2 = value3 = 0;

        if (!step.TryGetPropertyWrapper(ColumnKey.Speed, out var speedWrapper) ||
            !step.TryGetPropertyWrapper(ColumnKey.Setpoint, out var setpointWrapper) ||
            !step.TryGetPropertyWrapper(ColumnKey.InitialValue, out var initialWrapper))
        {
            errorString = "Failed to retrieve required properties";
            return false;
        }

        value1 = speedWrapper.PropertyValue.AsFloat;
        value2 = setpointWrapper.PropertyValue.AsFloat;
        value3 = initialWrapper.PropertyValue.AsFloat;

        return true;
    }

    private bool TryCalculateAndSetDuration(Step step, float speed, float setpoint, float initial, out string errorString)
    {
        errorString = null;

        if (speed == 0)
        {
            errorString = "Invalid speed value: cannot be zero";
            return false;
        }

        var calculatedTime = Math.Abs(setpoint - initial) * 60 / speed;
        return TrySetCalculatedProperty(step, ColumnKey.Duration, calculatedTime, PropertyType.Time, out errorString);
    }

    private bool TryCalculateAndSetSpeed(Step step, float duration, float setpoint, float initial, out string errorString)
    {
        errorString = null;

        if (duration == 0)
        {
            errorString = "Invalid duration value: cannot be zero";
            return false;
        }

        var calculatedSpeed = Math.Abs(setpoint - initial) * 60 / duration;
        return TrySetCalculatedProperty(step, ColumnKey.Speed, calculatedSpeed, GetSpeedPropertyType(step), out errorString);
    }

    private PropertyType GetSpeedPropertyType(Step step)
    {
        step.TryGetPropertyWrapper(ColumnKey.Action, out var actionWrapper);

        return actionWrapper.PropertyValue.AsInt switch
        {
            var id when id == _actionManager.PowerSmooth.Id => PropertyType.PowerSpeed,
            var id when id == _actionManager.TemperatureSmooth.Id => PropertyType.TempSpeed,
            _ => throw new InvalidOperationException("Unsupported action type for speed calculation")
        };
    }

    private bool TrySetCalculatedProperty(Step step, ColumnKey propertyKey, float value, PropertyType type, out string errorString)
    {
        var propertyValue = new PropertyValue(value, type, false);
        return step.TryChangePropertyValue(propertyKey, propertyValue, out errorString);
    }
}