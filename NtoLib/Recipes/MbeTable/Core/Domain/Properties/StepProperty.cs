#nullable enable

using System;
using System.Globalization;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties
{
    /// <summary>
    /// Represents a single, immutable recipe property.
    /// </summary>
    public record StepProperty
    {
        private record PropertyValue(OneOf<bool, int, float, string> UnionValue, PropertyType Type);

        private PropertyValue Value { get; init; }
        private PropertyDefinitionRegistry Registry { get; init; }

        internal StepProperty(object initialValue, PropertyType propertyType, PropertyDefinitionRegistry registry)
        {
            var definition = registry.GetDefinition(propertyType);
            if (initialValue.GetType() != definition.SystemType)
            {
                throw new ArgumentException(
                    $"Initial value type '{initialValue.GetType().Name}' does not match the expected system type '{definition.SystemType.Name}' for PropertyType '{propertyType}'.");
            }
            if (!definition.Validate(initialValue, out var errorMessage))
            {
                throw new ArgumentException($"Initial value '{initialValue}' is invalid for PropertyType '{propertyType}': {errorMessage}");
            }
            
            Registry = registry;
            Value = new PropertyValue(CreateUnionValue(initialValue), propertyType);
        }

        private StepProperty(PropertyValue value, PropertyDefinitionRegistry registry)
        {
            Value = value;
            Registry = registry;
        }

        public PropertyType Type => Value.Type;

        public (bool Success, StepProperty NewProperty, RecipePropertyError? Error) WithValue(object newValue)
        {
            var definition = Registry.GetDefinition(Type);
            
            object convertedValue;
            try
            {
                convertedValue = Convert.ChangeType(newValue, definition.SystemType, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return (false, this, new ConversionError(newValue.ToString() ?? "", definition.SystemType.Name));
            }
            
            if (!definition.Validate(convertedValue, out var validationMessage))
            {
                return (false, this, new ValidationError(validationMessage));
            }

            var newUnion = CreateUnionValue(convertedValue);
            var newPropertyValue = new PropertyValue(newUnion, Type);
            var newProperty = new StepProperty(newPropertyValue, Registry);

            return (true, newProperty, null);
        }

        public T GetValue<T>() where T : notnull
        {
            if (Value.UnionValue.Value is T typedValue) return typedValue;
            throw new InvalidCastException($"Cannot get value of type '{typeof(T).Name}' from property. Actual type is '{Value.UnionValue.Value.GetType().Name}'.");
        }
        
        public object GetValueAsObject() => Value.UnionValue.Value;
        
        public string GetDisplayValue()
        {
            var definition = Registry.GetDefinition(Type);
            return $"{definition.FormatValue(Value.UnionValue.Value)} {definition.Units}".Trim();
        }

        private static OneOf<bool, int, float, string> CreateUnionValue(object value)
        {
            return value switch
            {
                bool b => b,
                int i => i,
                float f => f,
                string s => s,
                _ => throw new InvalidOperationException($"Unsupported type '{value.GetType().Name}'.")
            };
        }
    }
}