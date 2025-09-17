#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Defines a contract for validating action definitions.
/// </summary>
public interface IActionDefsValidator
{
    /// <summary>
    /// Validates the provided collection of action definitions against the table schema.
    /// </summary>
    /// <param name="definitions">The action definitions to validate.</param>
    /// <param name="tableColumns">The table columns schema to check against.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Result Validate(IReadOnlyCollection<ActionDefinition> definitions, TableColumns tableColumns);
}