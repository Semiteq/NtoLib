using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;

public static class PropertyTypeMapper
{
    private static readonly Dictionary<PropertyType, Type> TypeMapping = new()
    {
        { PropertyType.Blocked, typeof(void) },
        { PropertyType.Bool, typeof(bool) },
        { PropertyType.Enum, typeof(Enum) },
        { PropertyType.Float, typeof(float) },
        { PropertyType.Flow, typeof(float) },
        { PropertyType.Int, typeof(int) },
        { PropertyType.Percent, typeof(float) },
        { PropertyType.PowerSpeed, typeof(float) },
        { PropertyType.String, typeof(string) },
        { PropertyType.Temp, typeof(float) },
        { PropertyType.TempSpeed, typeof(float) },
        { PropertyType.Time, typeof(float) }
    };

    public static Type GetSystemType(PropertyType propertyType)
    {
        return TypeMapping.TryGetValue(propertyType, out var systemType) 
            ? systemType 
            : throw new ArgumentException($"Unknown PropertyType: {propertyType}");
    }
}