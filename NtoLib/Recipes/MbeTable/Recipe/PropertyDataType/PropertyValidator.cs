using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyValidator
{
    public bool Validate(PropertyValue value, out string validationError)
    {
        validationError = string.Empty;

        var limits = GetLimits(value.Type);
        if (!limits.HasValue)
        {
            return true;
        }

        var (min, max) = limits.Value;

        if (value.IsBool) // bool
        {
            return true; // bool always valid
        }
        else if (value.IsInt) // int
        {
            var intValue = value.AsInt;
            if (intValue < min || intValue > max)
            {
                validationError = $"Целое число должно быть в пределах от {min} до {max}";
                return false;
            }
            return true;
        }
        else if (value.IsFloat) // float
        {
            var floatValue = value.AsFloat;
            if (floatValue < min || floatValue > max)
            {
                validationError = $"Число должно быть в пределах от {min} до {max}";
                return false;
            }
            return true;
        }
        else if (value.IsString) // string
        {
            var stringValue = value.AsString;
            if (stringValue.Length < min || stringValue.Length > max)
            {
                validationError = $"Длина строки должна быть от {min} до {max} символов";
                return false;
            }
            return true;
        }

        return true;
    }
        
    private (float min, float max)? GetLimits(PropertyType propertyType)
    {
        return _limits.TryGetValue(propertyType, out var limits) 
            ? limits 
            : throw new KeyNotFoundException($"Unknown PropertyType: {propertyType}");
    }
    
    private readonly Dictionary<PropertyType, (float min, float max)?> _limits = new()
    {
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
}