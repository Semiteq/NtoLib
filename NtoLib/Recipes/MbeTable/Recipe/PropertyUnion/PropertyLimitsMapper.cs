using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyUnion
{
    public static class PropertyLimitsMapper
    {
        private static readonly Dictionary<PropertyType, (float min, float max)?> Limits = new()
        {
            { PropertyType.Blocked, null },
            { PropertyType.Bool, null },
            { PropertyType.Enum, null },
            { PropertyType.Float, (float.MinValue, float.MaxValue) },
            { PropertyType.Flow, (0, 100000) },
            { PropertyType.Int, (int.MinValue, int.MaxValue) },
            { PropertyType.Percent, (0, 100) },
            { PropertyType.PowerSpeed, (-100, 100) },
            { PropertyType.String, (0, 255) },
            { PropertyType.Temp, (0, 2000) },
            { PropertyType.TempSpeed, (-1000, 1000) },
            { PropertyType.Time, (0, 86400) }
        };
        
        public static (float min, float max)? GetLimits(PropertyType propertyType)
        {
            return Limits.TryGetValue(propertyType, out var limits) 
                ? limits 
                : throw new KeyNotFoundException($"Unknown PropertyType: {propertyType}");
        }
    }
}