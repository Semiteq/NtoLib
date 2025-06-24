using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;

public static class PropertyUnitsMapper
{
    private static readonly Dictionary<PropertyType, string> Units = new()
    {
        { PropertyType.Blocked, "" },
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
    
    public static string GetUnits(PropertyType propertyType)
    {
        return Units.TryGetValue(propertyType, out var units) 
            ? units 
            : throw new ArgumentException($"Unknown PropertyType: {propertyType}");
    }
}