#nullable enable

using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using OneOf;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties
{
    /// <summary>
    /// Represents a single, immutable recipe property.
    /// </summary>
    public record StepProperty
    {
        private record PropertyValue(OneOf<bool, int, float, string> UnionValue, PropertyType Type);
        private PropertyValue Value { get; init; }
        private PropertyDefinitionRegistry PropertyRegistry { get; init; }

        /// <summary>
        /// Gets the <see cref="PropertyType"/> of this <see cref="StepProperty"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="PropertyType"/> defines the type of the property, such as
        /// <c>Int</c>, <c>Float</c>, <c>String</c>, <c>Bool</c>, or other supported
        /// enumeration values defined in the <see cref="PropertyType"/> enum.
        /// </remarks>
        /// <value>
        /// The type of the property as a <see cref="PropertyType"/>.
        /// </value>
        public PropertyType Type => Value.Type;

        /// <summary>
        /// Updates the property with a new value and validates it against its type definition.
        /// </summary>
        /// <param name="newValue">The new value to update the property with. Can be unformatted and contain strings.</param>
        /// <returns>A <see cref="Result{T}"/> containing the updated <see cref="StepProperty"/> if successful, or an <see cref="Error"/> if it fails.</returns>
        public Result<StepProperty> WithValue(object newValue)
        {
            var propertyTypeDefinition = PropertyRegistry.GetDefinition(Type);

            var (parseSuccess, parsedValue) = propertyTypeDefinition.TryParse(newValue.ToString() ?? string.Empty);
            if (!parseSuccess)
            {
                return Result.Fail(new ConversionError(newValue.ToString() ?? "", propertyTypeDefinition.SystemType.Name));
            }

            var (validationSuccess, errorMessage) = propertyTypeDefinition.Validate(parsedValue);
            if (!validationSuccess)
            {
                return Result.Fail(new ValidationError(errorMessage));
            }

            var newUnion = CreateUnionValue(parsedValue);
            var newPropertyValue = new PropertyValue(newUnion, Type);
            var newProperty = new StepProperty(newPropertyValue, PropertyRegistry);

            return Result.Ok(newProperty);
        }

        /// <summary>
        /// Retrieves the value stored in the step property and returns it as the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value to retrieve.</typeparam>
        /// <returns>The value of the step property cast to the specified type.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown when the stored value cannot be cast to the specified type.
        /// </exception>
        public T GetValue<T>() where T : notnull
        {
            if (Value.UnionValue.Value is T typedValue) return typedValue;
            throw new InvalidCastException($"Cannot get value of type '{typeof(T).Name}' from property. Actual type is '{Value.UnionValue.Value.GetType().Name}'.");
        }

        /// <summary>
        /// Retrieves the value of the property as a generic object.
        /// </summary>
        /// <returns>The value of the property represented as an object.</returns>
        public object GetValueAsObject() => Value.UnionValue.Value;

        /// <summary>
        /// Formats and returns the property value as a string, including its unit if applicable.
        /// </summary>
        /// <returns>The formatted value of the property with its unit, or the value alone if no unit is defined.
        /// Trailing spaces are removed from the result.</returns>
        public string GetDisplayValue()
        {
            var definition = PropertyRegistry.GetDefinition(Type);
            return $"{definition.FormatValue(Value.UnionValue.Value)} {definition.Units}".Trim();
        }

        /// <summary>
        /// Represents a single, immutable property belonging to a recipe step.
        /// </summary>
        public StepProperty(object initialValue, PropertyType propertyType,
            PropertyDefinitionRegistry propertyRegistry)
        {
            var definition = propertyRegistry.GetDefinition(propertyType);
            if (initialValue.GetType() != definition.SystemType)
            {
                throw new ArgumentException(
                    $"Initial value type '{initialValue.GetType().Name}' does not match the expected system type '{definition.SystemType.Name}' for PropertyType '{propertyType}'.");
            }

            var (validationSuccess, errorMessage) = definition.Validate(initialValue);
            if (!validationSuccess)
            {
                throw new ArgumentException($"Initial value '{initialValue}' is invalid for PropertyType '{propertyType}': {errorMessage}");
            }

            PropertyRegistry = propertyRegistry;
            Value = new PropertyValue(CreateUnionValue(initialValue), propertyType);
        }

        /// <summary>
        /// Represents a single, immutable property belonging to a recipe step.
        /// </summary>
        private StepProperty(PropertyValue value, PropertyDefinitionRegistry propertyRegistry)
        {
            Value = value;
            PropertyRegistry = propertyRegistry;
        }

        private OneOf<bool, int, float, string> CreateUnionValue(object value)
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