using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager;

public class DependencyRule
{
    public HashSet<ColumnKey> TriggerKeys { get; }
    public ColumnKey OutputKey { get; }
    public Func<IReadOnlyDictionary<ColumnKey, PropertyWrapper>, (object Value, string Error)> Calculation { get; }

    public DependencyRule(IEnumerable<ColumnKey> triggerKeys, ColumnKey outputKey, 
        Func<IReadOnlyDictionary<ColumnKey, PropertyWrapper>, (object, string)> calculation)
    {
        TriggerKeys = new HashSet<ColumnKey>(triggerKeys);
        OutputKey = outputKey;
        Calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
    }

    public bool TryApply(IReadOnlyDictionary<ColumnKey, PropertyWrapper> context, 
        PropertyWrapper targetProperty, out PropertyWrapper result, out string error)
    {
        var (calculatedValue, calculationError) = Calculation(context);
        
        if (calculationError != null)
        {
            result = null;
            error = calculationError;
            return false;
        }

        return targetProperty.TryChangeValue(calculatedValue, out result, out error);
    }
}