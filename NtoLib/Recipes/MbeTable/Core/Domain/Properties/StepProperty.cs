#nullable enable
using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties;

/// <summary>
/// Represents a single, immutable recipe property driven by a dynamic type id.
/// </summary>
public sealed record StepProperty
{
    private sealed record PropertyValue(OneOf<int, float, string> UnionValue, string PropertyTypeId);

    private PropertyValue Value { get; init; }
    private PropertyDefinitionRegistry PropertyRegistry { get; init; }

    /// <summary>
    /// Gets the dynamic property type id.
    /// </summary>
    public string PropertyTypeId => Value.PropertyTypeId;

    /// <summary>
    /// Updates the property with a new value and validates it against its type definition.
    /// </summary>
    public Result<StepProperty> WithValue(object newValue)
    {
        var def = PropertyRegistry.GetDefinition(PropertyTypeId);

        var parseResult = def.TryParse(newValue.ToString());
        if (parseResult.IsFailed)
            return Result.Fail(new ConversionError(newValue.ToString(), def.SystemType.Name));
        var parsedValue = parseResult.Value;
        
        
        var validationResult = def.TryValidate(parsedValue);
        if (validationResult.IsFailed)
            return Result.Fail(new ValidationError(validationResult.Errors));

        var newUnion = CreateUnionValue(parsedValue);
        var newPropertyValue = new PropertyValue(newUnion, PropertyTypeId);
        return Result.Ok(new StepProperty(newPropertyValue, PropertyRegistry));
    }

    /// <summary>
    /// Retrieves the value as T.
    /// </summary>
    public T GetValue<T>() where T : notnull
    {
        if (Value.UnionValue.Value is T typed) return typed;
        throw new InvalidCastException($"Cannot get value of type '{typeof(T).Name}' from property. Actual type is '{Value.UnionValue.Value.GetType().Name}'.");
    }

    /// <summary>
    /// Retrieves the underlying object value.
    /// </summary>
    public object GetValueAsObject() => Value.UnionValue.Value;

    /// <summary>
    /// Formats the value according to its definition.
    /// </summary>
    public string GetDisplayValue()
    {
        var definition = PropertyRegistry.GetDefinition(PropertyTypeId);
        return $"{definition.FormatValue(Value.UnionValue.Value)} {definition.Units}".Trim();
    }

    /// <summary>
    /// Initializes a property with explicit initial value and dynamic property type id.
    /// </summary>
    public StepProperty(object initialValue, string propertyTypeId, PropertyDefinitionRegistry propertyRegistry)
    {
        PropertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        var def = PropertyRegistry.GetDefinition(propertyTypeId);

        if (initialValue.GetType() != def.SystemType)
            throw new ArgumentException($"Initial value type '{initialValue.GetType().Name}' does not match expected '{def.SystemType.Name}' for '{propertyTypeId}'.");

        var (ok, msg) = def.TryValidate(initialValue);
        if (!ok)
            throw new ArgumentException($"Initial value '{initialValue}' is invalid for '{propertyTypeId}': {msg}");

        Value = new PropertyValue(CreateUnionValue(initialValue), propertyTypeId);
    }

    private StepProperty(PropertyValue value, PropertyDefinitionRegistry propertyRegistry)
    {
        Value = value;
        PropertyRegistry = propertyRegistry;
    }

    private static OneOf<int, float, string> CreateUnionValue(object value) =>
        value switch
        {
            int i => i,
            float f => f,
            string s => s,
            _ => throw new InvalidOperationException($"Unsupported type '{value.GetType().Name}'.")
        };
}