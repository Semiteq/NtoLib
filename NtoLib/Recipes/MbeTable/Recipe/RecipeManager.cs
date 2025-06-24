using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe;

public class RecipeManager
{
    private readonly List<Step> _recipe;
    
    private readonly StepFactory _stepFactory;
    private readonly ActionManager _actionManager;

    private readonly Dictionary<ColumnKey, int> _columnKeyToIndexMap;
    private readonly Dictionary<int, ColumnKey> _columnIndexToKeyMap;
    private readonly int _columnCount;
    
    private readonly ActionTarget _shutters;
    private readonly ActionTarget _heaters;
    private readonly ActionTarget _nitrogenSources;
    
    public RecipeManager(Dictionary<ColumnKey, int> columnKeyToIndexMap, Dictionary<int, ColumnKey> columnIndexToKeyMap, int columnCount)
    {
        _recipe = new List<Step>();
        
        _actionManager = new ActionManager();
        _stepFactory = new StepFactory(_actionManager);

        _shutters = new ActionTarget();
        _heaters = new ActionTarget();
        _nitrogenSources = new ActionTarget();
        
        _columnKeyToIndexMap = columnKeyToIndexMap ?? throw new ArgumentNullException(nameof(columnKeyToIndexMap), @"Column key to index map cannot be null");
        _columnIndexToKeyMap = columnIndexToKeyMap ?? throw new ArgumentNullException(nameof(columnIndexToKeyMap), @"Column index to key map cannot be null");
        _columnCount = columnCount;
    }

    public ActionTarget GetShutters() => _shutters;
    public ActionTarget GetHeaters() => _heaters;
    public ActionTarget GetNitrogenSources() => _nitrogenSources;
    
    public IReadOnlyList<Step> Steps => _recipe.AsReadOnly();
    
    public int StepCount => _recipe.Count;
    
    public bool TryAddNewStep(int index, out Step openStep, out string errorString)
    {
        openStep = null;
        errorString = null;
        
        index = Math.Max(0, Math.Min(index, _recipe.Count));
    
        if (!_stepFactory.TryCreateOpenStep(out openStep, out errorString, _shutters.GetMinimalId()))
        {
            return false;
        }
    
        _recipe.Insert(index, openStep);
        return true;
    }
    
    public bool TryRemoveStep(int index, out string errorString)
    {
        errorString = null;
        if (index < 0 || index >= _recipe.Count)
        {
            errorString = @"Index is out of range";
            return false;
        }

        _recipe.RemoveAt(index);
        return true;
    }
    
    public bool TryGetStep(int index, out Step step, out string errorString)
    {
        errorString = null;
        step = null;

        if (index < 0 || index >= _recipe.Count)
        {
            errorString = @"Index is out of range";
            return false;
        }

        step = _recipe[index];
        return true;
    }

    public bool TrySetStepProperty(int rowIndex, int columnIndex, object value, out string formatedValue, out string errorString)
    {
        errorString = string.Empty;
        formatedValue = string.Empty;
        
        _columnIndexToKeyMap.TryGetValue(columnIndex, out var columnKey);
        
        var step = _recipe[rowIndex];
        var stepType = step.TryGetProperty(columnKey).SystemType;
    
        try
        {
            object convertedValue = stepType switch
            {
                not null when stepType == typeof(int) => Convert.ToInt32(value),
                not null when stepType == typeof(float) => Convert.ToSingle(value),
                not null when stepType == typeof(string) => value?.ToString() ?? string.Empty,
                not null when stepType == typeof(bool) => Convert.ToBoolean(value),
                _ => throw new NotSupportedException($"Unsupported type: {stepType}")
            };
            
            return step.TrySetProperty(columnKey, convertedValue, out errorString);
        }
        catch (Exception ex)
        {
            errorString = $"Type conversion failed: {ex.Message}";
            return false;
        }
    }

    private int GetColumnIndex(ColumnKey key) => _columnKeyToIndexMap.FirstOrDefault(kvp => kvp.Key == key).Value;
    private ColumnKey GetColumnKey(int index) => _columnIndexToKeyMap.FirstOrDefault(kvp => kvp.Key == index).Value;
    

    /// <summary>
    /// MBE specific recalculation logic for columns that depend on other columns.
    /// </summary>
    private bool IsRecalculationRequired(int columnIndex, Step step)
    {
        var isRelevantColumn = columnIndex == GetColumnIndex(ColumnKey.Setpoint)
                               || columnIndex == GetColumnIndex(ColumnKey.Speed)
                               || columnIndex == GetColumnIndex(ColumnKey.InitialValue);

        if (!isRelevantColumn) return false;

        var actionEntry = step.ActionEntry;
        return actionEntry == _actionManager.PowerSmooth
               || actionEntry == _actionManager.TemperatureSmooth;
    }

    private bool TryProcessTime(int rowIndex, out string errorString)
    {
        errorString = null;
        if (rowIndex < 0 || rowIndex >= _recipe.Count)
        {
            errorString = @"Row index is out of range";
            return false;
        }

        var step = _recipe[rowIndex];

        // Skip processing for wait actions
        if (step.ActionEntry == _actionManager.TemperatureWait ||
            step.ActionEntry == _actionManager.PowerWait) return true;

        var setpoint = step.TryGetProperty(ColumnKey.Setpoint).FloatValue;
        var initial = step.TryGetProperty(ColumnKey.InitialValue).FloatValue;
        var speed = step.TryGetProperty(ColumnKey.Speed).FloatValue;

        if (speed != 0 && !float.IsNaN(Math.Abs(setpoint - initial) * 60 / speed))
        {
            var calculatedTime = Math.Abs(setpoint - initial) * 60 / speed;

            if (step.TrySetProperty(ColumnKey.Duration, calculatedTime, out errorString))
            {
                return true;
            }
        }
        
        errorString = @"Invalid speed value";
        return false;
    }
    
    private bool TryProcessSpeed(int rowIndex, out string errorString)
    {
        errorString = null;
        if (rowIndex < 0 || rowIndex >= _recipe.Count)
        {
            errorString = @"Row index is out of range";
            return false;
        }

        var step = _recipe[rowIndex];

        // Skip processing for wait actions
        if (step.ActionEntry == _actionManager.TemperatureWait ||
            step.ActionEntry == _actionManager.PowerWait) return true;

        var setpoint = step.TryGetProperty(ColumnKey.Setpoint).FloatValue;
        var initial = step.TryGetProperty(ColumnKey.InitialValue).FloatValue;
        var duration = step.TryGetProperty(ColumnKey.Duration).FloatValue;

        if (duration != 0 && !float.IsNaN(Math.Abs(setpoint - initial) * 60 / duration))
        {
            var calculatedSpeed = Math.Abs(setpoint - initial) * 60 / duration;

            if (step.TrySetProperty(ColumnKey.Speed, calculatedSpeed, out errorString))
            {
                return true;
            }
        }
        
        errorString = @"Invalid duration value";
        return false;
    }

    private bool TryHandleActionTargetEdit(int rowIndex, string targetValue, out string errorString)
    {
        errorString = null;
        if (rowIndex < 0 || rowIndex >= _recipe.Count)
        {
            errorString = @"Row index is out of range";
            return false;
        }

        var step = _recipe[rowIndex];
        
        return step.TrySetProperty(ColumnKey.ActionTarget, targetValue ?? "", out errorString);
    }
    
    private bool TryValidateIndices(int rowIndex, int columnIndex, out string errorString)
    {
        errorString = null;
        if (rowIndex < 0 || rowIndex >= _recipe.Count)
        {
            errorString = @"Row index is out of range";
            return false;
        }

        if (columnIndex < 0 || columnIndex >= _columnCount)
        {
            errorString = @"Column index is out of range";
            return false;
        }

        return true;
    }
}