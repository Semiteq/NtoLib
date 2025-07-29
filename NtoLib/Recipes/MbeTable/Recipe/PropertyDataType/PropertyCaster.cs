using System;
using System.Collections.Generic;
using System.Globalization;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyCaster
{
    public OneOf<bool, int, float, string> CastToUnionValue(object value, PropertyType type)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), @"Value cannot be null.");

        return type switch
        {
            PropertyType.Bool when value is bool boolValue => boolValue,
            PropertyType.Int when value is int intValue => intValue,
            PropertyType.Float when value is float floatValue => floatValue,
            PropertyType.String when value is string stringValue => stringValue,
            _ => throw new InvalidCastException($"Cannot cast {value.GetType()} to {type}.")
        };
    }
    
    public object Cast(PropertyValue value, Type targetType)
    {
        return value.UnionValue.Match<object>(
            boolValue => targetType == typeof(bool) ? boolValue : throw new InvalidCastException(),
            intValue => targetType == typeof(int) ? intValue : throw new InvalidCastException(),
            floatValue => targetType == typeof(float) ? floatValue : throw new InvalidCastException(),
            stringValue => targetType == typeof(string) ? stringValue : throw new InvalidCastException()
        );
    }
    
    public bool TryGetValue<T>(PropertyValue propertyValue, out T value)
    {
        if (propertyValue.IsBlocked)
        {
            value = default;
            return false;
        }

        try
        {
            value = propertyValue.UnionValue.Match(
                boolValue => (T)(object)boolValue,
                intValue => (T)(object)intValue,
                floatValue => (T)(object)floatValue,
                stringValue => (T)(object)stringValue
            );
            return true;
        }
        catch (InvalidCastException)
        {
            value = default;
            return false;
        }
    }

    public string GetRawValue(PropertyValue propertyValue)
    {
        return propertyValue.UnionValue.Match(
            boolValue => boolValue.ToString(),
            intValue => intValue.ToString(),
            floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            stringValue => stringValue
        );
    }
    
    public OneOf<bool, int, float, string> ConvertFromString(string value, PropertyType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return type switch
            {
                PropertyType.Bool => false,
                PropertyType.Int => 0,
                PropertyType.Float or PropertyType.Flow or PropertyType.Percent or 
                    PropertyType.PowerSpeed or PropertyType.Temp or PropertyType.TempSpeed or 
                    PropertyType.Time => 0f,
                PropertyType.String or PropertyType.Enum => string.Empty,
                _ => throw new ArgumentException($"Unknown PropertyType: {type}")
            };
        }

        return type switch
        {
            PropertyType.Bool => bool.Parse(value),
            PropertyType.Int => int.Parse(value),
            PropertyType.Float or PropertyType.Flow or PropertyType.Percent or 
                PropertyType.PowerSpeed or PropertyType.Temp or PropertyType.TempSpeed or 
                PropertyType.Time => float.Parse(value, CultureInfo.InvariantCulture),
            PropertyType.String or PropertyType.Enum => value,
            _ => throw new ArgumentException($"Unknown PropertyType: {type}")
        };
    }
    
    private Type GetSystemType(PropertyType propertyType)
    {
        return _typeMapping.TryGetValue(propertyType, out var systemType) 
            ? systemType 
            : throw new ArgumentException($"Unknown PropertyType: {propertyType}");
    }
    
    private readonly Dictionary<PropertyType, Type> _typeMapping = new()
    {
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
}