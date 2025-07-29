#nullable enable
using System;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;

public class PropertyWrapper
{
    public PropertyValue? PropertyValue { get; private set; }
    
    private readonly PropertyDefinitionRegistry _registry;
    
    public PropertyWrapper(PropertyValue propertyValue, PropertyDefinitionRegistry registry)
    {
        PropertyValue = propertyValue ?? throw new ArgumentNullException(nameof(propertyValue));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    // Blocked state
    public PropertyWrapper(PropertyDefinitionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        PropertyValue = null;
    }
    
    public PropertyType Type => PropertyValue?.Type 
        ?? throw new InvalidOperationException("Перманентно заблокированное свойство не имеет типа.");
    
    public bool IsBlocked => PropertyValue == null || PropertyValue.IsBlocked;

    

    public bool TryChangeValue<T>(T value, out string errorMessage)
    {
        if (PropertyValue == null)
        {
            errorMessage = "Невозможно изменить перманентно заблокированное свойство.";
            return false;
        }

        if (PropertyValue.IsBlocked)
        {
            errorMessage = "Невозможно изменить значение заблокированного свойства.";
            return false;
        }

        var definition = _registry.GetDefinition(PropertyValue.Type);
        if (typeof(T) != definition.SystemType)
        {
            errorMessage = $"Неверный тип данных. Свойство '{Type}' ожидает тип '{definition.SystemType.Name}', а был передан '{typeof(T).Name}'.";
            return false;
        }
        
        if (!definition.Validate(value, out errorMessage))
        {
            return false;
        }
        
        var newUnionValue = CreateUnionValue(value);
        PropertyValue = new PropertyValue(newUnionValue, PropertyValue.Type, PropertyValue.IsBlocked);
        
        errorMessage = string.Empty;
        return true;
    }

    public bool TryGetValue<T>(out T value)
    {
        if (IsBlocked)
        {
            value = default;
            return false;
        }

        if (PropertyValue.UnionValue.Value is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public bool TrySetValueFromString(string value, out string errorMessage)
    {
        if (PropertyValue == null)
        {
            errorMessage = "Невозможно изменить перманентно заблокированное свойство.";
            return false;
        }
        
        if (IsBlocked)
        {
            errorMessage = "Невозможно изменить значение заблокированного свойства.";
            return false;
        }

        var definition = _registry.GetDefinition(Type);
        
        if (!definition.TryParse(value, out var parsedValue))
        {
            errorMessage = $"Не удалось преобразовать строку '{value}' в тип '{Type}'.";
            return false;
        }
        
        if (!definition.Validate(parsedValue, out errorMessage))
        {
            return false;
        }
        
        var newUnionValue = CreateUnionValue(parsedValue);
        PropertyValue = new PropertyValue(newUnionValue, Type, IsBlocked);
        
        return true;
    }

    public string GetDisplayValue()
    {
        if (PropertyValue == null || PropertyValue.IsBlocked) return "";

        var definition = _registry.GetDefinition(PropertyValue.Type);
        string formattedValue = definition.FormatValue(PropertyValue.UnionValue.Value);
        
        return $"{formattedValue} {definition.Units}".Trim();
    }
    
    public bool IsValid(out string errorMessage)
    {
        if (PropertyValue == null)
        {
            errorMessage = "Перманентно заблокированное свойство не имеет значения для валидации.";
            return false; 
        }
        
        var definition = _registry.GetDefinition(PropertyValue.Type);
        return definition.Validate(PropertyValue.UnionValue.Value, out errorMessage);
    }
    
    private OneOf<bool, int, float, string> CreateUnionValue(object value)
    {
        return value switch
        {
            bool b => b,
            int i => i,
            float f => f,
            string s => s,
            _ => throw new InvalidOperationException($"Неподдерживаемый тип '{value?.GetType().Name}' для создания UnionValue.")
        };
    }
    
    public override string ToString() => GetDisplayValue();
}