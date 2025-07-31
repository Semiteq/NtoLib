using System;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
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
        
        var actionWrapper = step.GetProperty(ColumnKey.Action);
        
        if (!IsSmoothAction(actionWrapper.GetValue<int>()))
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

    private bool TryRecalculateDuration(Step step, out string errorString)
    {
        float[] values = GetValuesForCalculation(step);
        return TryCalculateAndSetDuration(step, values, out errorString);
    }

    private bool TryRecalculateSpeed(Step step, out string errorString)
    {
        float[] values = GetValuesForCalculation(step);
        return TryCalculateAndSetSpeed(step, values, out errorString);
    }

    private float[] GetValuesForCalculation(Step step)
    {
        var speedValue = step.GetProperty(ColumnKey.Speed).GetValue<float>();
        var setpointValue = step.GetProperty(ColumnKey.Setpoint).GetValue<float>();
        var initialValue = step.GetProperty(ColumnKey.InitialValue).GetValue<float>();

        return new[] { speedValue, setpointValue, initialValue };
    }

    private bool TryCalculateAndSetDuration(Step step, float[] values, out string errorString)
    {
        errorString = null;

        var speed = values[0];
        var setpoint = values[1];
        var initial = values[2];
        
        if (speed == 0)
        {
            errorString = "Invalid speed value: cannot be zero";
            return false;
        }

        var calculatedTime = Math.Abs(setpoint - initial) * 60 / speed;
        return TrySetCalculatedProperty(step, ColumnKey.Duration, calculatedTime, out errorString);
    }

    private bool TryCalculateAndSetSpeed(Step step, float[] values, out string errorString)
    {
        errorString = null;

        var duration = values[0];
        var setpoint = values[1];
        var initial = values[2];
        
        if (duration == 0)
        {
            errorString = "Invalid duration value: cannot be zero";
            return false;
        }

        var calculatedSpeed = Math.Abs(setpoint - initial) * 60 / duration;
        return TrySetCalculatedProperty(step, ColumnKey.Speed, calculatedSpeed, out errorString);
    }

    private bool TrySetCalculatedProperty(Step step, ColumnKey columnKey, float value, 
        out string errorString)
    {
        return step.TryChangePropertyValue(columnKey, value, out errorString);
    }
}