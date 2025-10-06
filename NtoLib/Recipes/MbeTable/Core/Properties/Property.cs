using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using OneOf;
using FluentResults;

using NtoLib.Recipes.MbeTable.Core.Properties.Contracts;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Properties;

public sealed record Property
{
    private OneOf<short, float, string> InternalUnionValue { get; init; }
    private IPropertyTypeDefinition PropertyDefinition { get; init; }
    public object GetValueAsObject() => InternalUnionValue.Value;
    public string GetDisplayValue() => $"{PropertyDefinition.FormatValue(InternalUnionValue.Value)} {PropertyDefinition.Units}".Trim();

    /// <exception cref="TypeAccessException">If value type does not match expected</exception>
    /// <exception cref="ValidationException">If value not passing validation</exception>
    /// <exception cref="FormatException">If value not passing conversion</exception>
    public Property(object value, IPropertyTypeDefinition propertyDefinition)
    {
        PropertyDefinition = propertyDefinition;
        if (value.GetType() != PropertyDefinition.SystemType)
            throw new TypeAccessException(
                $"Initial value type '{value.GetType().Name}' does not match expected '{PropertyDefinition.SystemType.Name}");

        var validationResult = PropertyDefinition.TryValidate(value);
        if (validationResult.IsFailed)
            throw new ValidationException(
                $"Can't create property with initial value '{value}'. Validation failed: {string.Join("; ", validationResult.Errors.Select(e => e.Message))}");

        var conversionResult = ConvertObjectToUnion(value);
        if (conversionResult.IsFailed)
            throw new FormatException(
                $"Can't create property with initial value '{value}'. Conversion failed: {string.Join("; ", conversionResult.Errors.Select(e => e.Message))}");

        InternalUnionValue = conversionResult.Value;
    }

    public Result<Property> WithValue(object newValue)
    {
        var parseResult = PropertyDefinition.TryParse(newValue.ToString());
        if (parseResult.IsFailed)
        {
            return Result.Fail<Property>(
                new Error($"Failed to convert '{newValue}' to type {PropertyDefinition.SystemType.Name}.")
                    .WithMetadata("code", ErrorCode.PropertyConversionFailed)
                    .CausedBy(parseResult.Errors));
        }

        var validationResult = PropertyDefinition.TryValidate(parseResult.Value);
        if (validationResult.IsFailed)
        {
            return Result.Fail<Property>(
                new Error(
                        $"Value '{parseResult.Value}' is invalid for type {PropertyDefinition.SystemType.Name} : {string.Join("; ", validationResult.Errors.Select(e => e.Message))}")
                    .WithMetadata("code", ErrorCode.PropertyValidationFailed)
                    .CausedBy(validationResult.Errors));
        }

        var conversionResult = ConvertObjectToUnion(parseResult.Value);
        if (conversionResult.IsFailed)
            return Result.Fail<Property>(conversionResult.Errors);

        return Result.Ok(new Property(conversionResult.Value.Value, PropertyDefinition));
    }
    
    /// <exception cref="InvalidCastException">If a requested type is not matching stored value type</exception>
    public T GetValue<T>() where T : notnull
    {
        return InternalUnionValue.Match(
            shortValue => shortValue is T typed ? typed : throw new InvalidCastException($"Value '{shortValue}' of type 'Int16' cannot be cast to type '{typeof(T).Name}'."),
            floatValue => floatValue is T typed ? typed : throw new InvalidCastException($"Value '{floatValue}' of type 'Single' cannot be cast to type '{typeof(T).Name}'."),
            stringValue => stringValue is T typed ? typed : throw new InvalidCastException($"Value '{stringValue}' of type 'String' cannot be cast to type '{typeof(T).Name}'.")
        );
    }

    
    private Result<OneOf<short, float, string>> ConvertObjectToUnion(object value) =>
        value switch
        {
            short i => Result.Ok<OneOf<short, float, string>>(i),
            float f => Result.Ok<OneOf<short, float, string>>(f),
            string s => Result.Ok<OneOf<short, float, string>>(s),
            _ => Result.Fail(new Error($"Unsupported type '{value.GetType().Name}' for property value.")
                .WithMetadata("code", ErrorCode.PropertyConversionFailed))
        };
}