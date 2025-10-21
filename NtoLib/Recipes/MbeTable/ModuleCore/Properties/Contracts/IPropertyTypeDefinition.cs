using System;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

public interface IPropertyTypeDefinition
{
    /// <summary>
    /// Gets the unit of measurement or description associated with the property.
    /// </summary>
    string Units { get; }

    /// <summary>
    /// Gets the system type associated with the property.
    /// </summary>
    Type SystemType { get; }

    /// <summary>
    /// Gets the formatting strategy for this property type.
    /// </summary>
    FormatKind FormatKind { get; }

    /// <summary>
    /// Validates the given value against the rules defined by the property type implementation.
    /// </summary>
    Result TryValidate(object value);

    /// <summary>
    /// Formats the given value according to the FormatKind strategy.
    /// </summary>
    string FormatValue(object value);

    /// <summary>
    /// Attempts to parse the input string into an object.
    /// </summary>
    Result<object> TryParse(string input);
}