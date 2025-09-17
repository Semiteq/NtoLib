#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Defines a contract for validating property definitions.
/// </summary>
public interface IPropertyDefsValidator
{
    /// <summary>
    /// Validates the provided collection of property definitions.
    /// </summary>
    /// <param name="definitions">The property definitions to validate.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Result Validate(IReadOnlyCollection<YamlPropertyDefinition> definitions);
}