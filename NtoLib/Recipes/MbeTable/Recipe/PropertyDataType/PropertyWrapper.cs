using System;
using System.Globalization;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Contracts;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyWrapper : ITableProperty
{
    public PropertyValue PropertyValue { get; private set; }
    public PropertyType Type => PropertyValue.Type;

    public bool IsBlocked => PropertyValue.IsBlocked;

    private readonly PropertyValidator _validator = new();
    private readonly PropertyCaster _caster = new();
    private readonly PropertyFormatter _formatter = new();

    public PropertyWrapper(PropertyValue propertyValue)
    {
        PropertyValue = propertyValue ?? throw new ArgumentNullException(nameof(propertyValue));
    }

    public PropertyWrapper(OneOf<bool, int, float, string> unionValue, PropertyType type, bool isBlocked = false)
    {
        // Конструктор не требует валидации, так как создается из метода со значением по умолчанию.
        PropertyValue = new PropertyValue(unionValue, type, isBlocked);
    }

    public bool TryChangeValue(PropertyValue newValue, out string errorString)
    {
        if (PropertyValue.IsBlocked)
        {
            errorString = $"Cannot set value on a blocked property.";
            return false;
        }

        if (!_validator.Validate(newValue, out errorString))
        {
            errorString = $"Validation failed: {errorString}";
            return false;
        }

        PropertyValue = newValue;
        return true;
    }
    
    public bool TryChangeValue(OneOf<bool, int, float, string> unionValue, out string errorString)
    {
        var newValue = new PropertyValue(unionValue, Type, IsBlocked);
        return TryChangeValue(newValue, out errorString);
    }
    
    public bool TryChangeValue(object value, out string errorString)
    {
        try
        {
            var unionValue = _caster.CastToUnionValue(value, Type);
            return TryChangeValue(unionValue, out errorString);
        }
        catch (Exception ex)
        {
            errorString = $"Failed to cast value: {ex.Message}";
            return false;
        }
    }
    
    public bool TryGetValue<T>(out T value)
        => _caster.TryGetValue(PropertyValue, out value);

    public string GetDisplayValue()
        => _formatter.Format(PropertyValue);

    public string GetRawValue()
        => _caster.GetRawValue(PropertyValue);

    public bool TrySetValueFromString(string value, out string errorMessage)
    {
        if (IsBlocked)
        {
            errorMessage = "Cannot set value on a blocked property.";
            return false;
        }

        try
        {
            var convertedValue = _caster.ConvertFromString(value, Type);
            var newPropertyValue = new PropertyValue(convertedValue, Type, IsBlocked);

            if (!_validator.Validate(newPropertyValue, out errorMessage))
            {
                return false;
            }

            PropertyValue = newPropertyValue;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to convert value: {ex.Message}";
            return false;
        }
    }

    public bool IsValid(out string errorMessage)
        => _validator.Validate(PropertyValue, out errorMessage);

    public object CastTo(Type targetType)
        => _caster.Cast(PropertyValue, targetType);

    public ITableProperty Clone()
        => new PropertyWrapper(PropertyValue.UnionValue, PropertyValue.Type, PropertyValue.IsBlocked);


    public override string ToString() => GetDisplayValue();
}