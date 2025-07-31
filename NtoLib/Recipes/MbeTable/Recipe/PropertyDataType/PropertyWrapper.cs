#nullable enable
using System;
using System.Globalization;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyWrapper
{
    private PropertyValue? _propertyValue;
    public bool IsBlocked => _propertyValue == null;

    private readonly PropertyDefinitionRegistry _registry;
    
    public PropertyWrapper(PropertyValue propertyValue, PropertyDefinitionRegistry registry)
    {
        _propertyValue = propertyValue ?? throw new ArgumentNullException(nameof(propertyValue));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    // Blocked state
    public PropertyWrapper(PropertyDefinitionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _propertyValue = null;
    }

    public PropertyType Type()
    {
        if (IsBlocked)
            throw new InvalidOperationException("Перманентно заблокированное свойство не имеет типа.");
        
        return _propertyValue.Type;
    }

    public bool TryChangeValue<T>(T value, out string errorString) where T : notnull
    {
        errorString = string.Empty;
        
        if (value == null)
            throw new ArgumentNullException(nameof(value), @"Значение не может быть null.");
        
        if (IsBlocked)
            throw new InvalidOperationException("Невозможно изменить значение заблокированного свойства.");
        
        var definition = _registry.GetDefinition(_propertyValue.Type);
        if (typeof(T) != definition.SystemType)
        {
            throw new InvalidCastException(
                $"Неверный тип данных. Свойство '{Type}' ожидает тип '{definition.SystemType.Name}', а был передан '{typeof(T).Name}'.");
        }
        
        if (!definition.Validate(value, out var errorMessage))
        {
            errorString = errorMessage;
            return false;
        }
        
        var newUnionValue = CreateUnionValue(value);
        _propertyValue = new PropertyValue(newUnionValue, _propertyValue.Type);
        
        return true;
    }
    
    public bool TryChangeValue(object value, out string errorMessage)
    {
        if (value == null)
        {
            errorMessage = "Значение не может быть null.";
            throw new ArgumentNullException(nameof(value), errorMessage);
        }
        
        if (IsBlocked)
        {
            errorMessage = "Невозможно изменить значение заблокированного свойства.";
            throw new InvalidOperationException(errorMessage);
        }

        var definition = _registry.GetDefinition(_propertyValue.Type);
        var targetType = definition.SystemType;

        object convertedValue;
        try
        {
            convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
        {
            throw new InvalidCastException(
                $"Не удалось преобразовать значение '{value}' (тип: {value.GetType().Name}) в целевой тип '{targetType.Name}'.", ex);
        }
        
        if (!definition.Validate(convertedValue, out errorMessage))
            return false;
        
        
        var newUnionValue = CreateUnionValue(convertedValue);
        _propertyValue = new PropertyValue(newUnionValue, _propertyValue.Type);
        
        errorMessage = string.Empty;
        return true;
    }

    public T GetValue<T>() where T : notnull
    {
        if (IsBlocked)
            throw new InvalidOperationException("Невозможно получить значение заблокированного свойства.");
        
        if (_propertyValue.UnionValue.Value is not T typedValue)
            throw new InvalidOperationException($"Невозможно получить значение типа '{typeof(T).Name}' из свойства типа '{_propertyValue.Type}'.");
        
        return typedValue;
    }

    public object GetValue()
    {
        if (IsBlocked)
            throw new InvalidOperationException("Невозможно получить значение заблокированного свойства.");
        
        return _propertyValue.UnionValue.Value;
    }

    public string GetDisplayValue()
    {
        if (IsBlocked)
        {
            throw new InvalidOperationException("Невозможно получить значение заблокированного свойства.");
        }

        var definition = _registry.GetDefinition(_propertyValue.Type);
        string formattedValue = definition.FormatValue(_propertyValue.UnionValue.Value);
        
        return $"{formattedValue} {definition.Units}".Trim();
    }
    
    public override string ToString() => GetDisplayValue();
    
    public bool IsValid(out string errorMessage)
    {
        if (IsBlocked)
        {
            throw new InvalidOperationException("Невозможно проверить валидность заблокированного свойства.");
        }
        
        var definition = _registry.GetDefinition(_propertyValue.Type);
        return definition.Validate(_propertyValue.UnionValue.Value, out errorMessage);
    }
    
    private OneOf<bool, int, float, string> CreateUnionValue(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Значение не может быть null при создании UnionValue.");
        
        return value switch
        {
            bool b => b,
            int i => i,
            float f => f,
            string s => s,
            _ => throw new InvalidOperationException($"Неподдерживаемый тип '{value?.GetType().Name}' для создания UnionValue.")
        };
    }
}