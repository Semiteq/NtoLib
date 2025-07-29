using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyFormatter
{
    public string Format(PropertyValue value)
    {
        var units = GetUnits(value.Type);
        return value.IsBlocked ? string.Empty : $"{value.UnionValue} {units}".Trim();
    }
    
    private string GetUnits(PropertyType propertyType)
    {
        return _units.TryGetValue(propertyType, out var units) 
            ? units 
            : throw new ArgumentException($"Unknown PropertyType: {propertyType}");
    }
    
    private readonly Dictionary<PropertyType, string> _units = new()
    {
        { PropertyType.Bool, "" },
        { PropertyType.Enum, "" },
        { PropertyType.Float, "°C" },
        { PropertyType.Flow, "см³/мин" },
        { PropertyType.Int, "" },
        { PropertyType.Percent, "%" },
        { PropertyType.PowerSpeed, "Вт/мин" },
        { PropertyType.String, "" },
        { PropertyType.Temp, "°C" },
        { PropertyType.TempSpeed, "°C/мин" },
        { PropertyType.Time, "с" }
    };
}