#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Actions;
using NtoLib.Recipes.MbeTable.Errors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;

/// <summary>
/// Loads action definitions from a YAML configuration file.
/// </summary>
public sealed class ActionDefsLoader : IActionDefsLoader
{
    /// <inheritdoc />
    public Result<IReadOnlyList<ActionDefinition>> LoadActions(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return Result.Fail(new RecipeError($"Actions configuration file not found at: '{configPath}'", RecipeErrorCodes.ConfigFileNotFound));

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var list = deserializer.Deserialize<List<ActionDefinition>>(yaml) ?? new();
            
            return Result.Ok<IReadOnlyList<ActionDefinition>>(list);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Error parsing actions YAML: {ex.Message}", RecipeErrorCodes.ConfigParseError).CausedBy(ex));
        }
    }
}