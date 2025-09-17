#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;
using NtoLib.Recipes.MbeTable.Errors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;

/// <summary>
/// YAML-based loader for property definitions.
/// </summary>
public sealed class PropertyDefinitionLoader : IPropertyDefinitionLoader
{
    /// <inheritdoc/>
    public Result<IReadOnlyList<YamlPropertyDefinition>> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result.Fail(new RecipeError("Empty path for PropertyDefinitions.yaml.", RecipeErrorCodes.ConfigInvalidSchema));

        try
        {
            if (!File.Exists(path))
                return Result.Fail(new RecipeError($"Config file not found: '{path}'", RecipeErrorCodes.ConfigFileNotFound));

            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance) // <--- ИЗМЕНЕНО
                .IgnoreUnmatchedProperties()
                .Build();

            var items = deserializer.Deserialize<List<YamlPropertyDefinition>>(yaml) ?? new List<YamlPropertyDefinition>();
            
            return Result.Ok<IReadOnlyList<YamlPropertyDefinition>>(items);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Failed to parse property definitions YAML: {ex.Message}", RecipeErrorCodes.ConfigParseError).CausedBy(ex));
        }
    }
}