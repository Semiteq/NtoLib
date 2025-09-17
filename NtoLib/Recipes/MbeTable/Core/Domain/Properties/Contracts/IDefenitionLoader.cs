#nullable enable
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

/// <summary>
/// Loads property type definitions from configuration (YAML).
/// </summary>
public interface IPropertyDefinitionLoader
{
    /// <summary>
    /// Loads property definitions and materializes IPropertyTypeDefinition instances keyed by PropertyTypeId.
    /// </summary>
    /// <param name="path">Full path to PropertyDefinitions.yaml.</param>
    /// <returns>Dictionary of type id to definition.</returns>
    Result<IReadOnlyList<YamlPropertyDefinition>> Load(string path);
}