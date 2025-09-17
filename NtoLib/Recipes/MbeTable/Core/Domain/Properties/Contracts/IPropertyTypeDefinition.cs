using System;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

public interface IPropertyTypeDefinition
{
    /// Gets the unit of measurement or description associated with the property.
    /// This property provides context for interpreting the property's value,
    /// such as specifying "cm", "kg", or other relevant units.
    /// Returns an empty string if units are not applicable or explicitly defined.
    string Units { get; }

    /// Gets the system type associated with the property. This property
    /// represents the fundamental .NET type (e.g., string, int or float (Single).
    Type SystemType { get; }

    /// <summary>
    /// Validates the given value against the rules defined by the property type implementation.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <returns>A result indicating whether the validation was successful or providing the validation errors if not.</returns>
    Result TryValidate(object value);

    /// <summary>
    /// Formats the given value according to the specific implementation details of the property type definition.
    /// </summary>
    /// <param name="value">The value to be formatted.</param>
    /// <returns>A string representation of the formatted value.</returns>
    string FormatValue(object value);

    /// <summary>
    /// Attempts to parse the input string into an object based on the specific implementation details
    /// of the property type definition.
    /// </summary>
    /// <param name="input">The string to parse into an object.</param>
    /// <returns>A <see cref="Result{T}"/> containing the parsed object if successful,
    /// or an error result if parsing fails.</returns>
    Result<object> TryParse(string input);
}