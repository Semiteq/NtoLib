#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;
using NtoLib.Recipes.MbeTable.Errors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;

/// <summary>
/// YAML loader for PinGroupDefs.yaml.
/// </summary>
public sealed class PinGroupDefsLoader : IPinGroupDefsLoader
{
    /// <inheritdoc />
    public Result<IReadOnlyList<PinGroupData>> LoadPinGroups(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return Result.Fail(new RecipeError($"Pin groups file not found at: '{configPath}'", RecipeErrorCodes.ConfigFileNotFound));

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance) // <--- ИЗМЕНЕНО
                .IgnoreUnmatchedProperties()
                .Build();

            var groups = deserializer.Deserialize<List<PinGroupData>>(yaml);

            if (groups == null || groups.Count == 0)
                return Result.Fail(new RecipeError($"{Path.GetFileName(configPath)} is empty or invalid.", RecipeErrorCodes.ConfigInvalidSchema));

            return Result.Ok<IReadOnlyList<PinGroupData>>(groups);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Error parsing {Path.GetFileName(configPath)}: {ex.Message}", RecipeErrorCodes.ConfigParseError).CausedBy(ex));
        }
    }
}