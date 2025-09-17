#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

public interface IColumnDefsValidator
{
    /// <summary>
    /// Validates a collection of YAML column definitions against a property definition registry.
    /// </summary>
    /// <param name="definitions">The collection of YAML column definitions to validate.</param>
    /// <param name="registry">The registry containing property type definitions used for validation.</param>
    /// <returns>A result indicating success or failure, with associated error messages if validation fails.</returns>
    Result Validate(IReadOnlyCollection<YamlColumnDefinition> definitions, PropertyDefinitionRegistry registry);
}