#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Errors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;

public sealed class ColumnDefsLoader : IColumnDefsLoader
{
    public Result<IReadOnlyList<YamlColumnDefinition>> LoadColumnDefs(string schemaPath)
    {
        try
        {
            if (!File.Exists(schemaPath))
                return Result.Fail(new RecipeError($"Configuration file not found at: '{schemaPath}'", RecipeErrorCodes.ConfigFileNotFound));

            var yaml = File.ReadAllText(schemaPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var items = deserializer.Deserialize<List<YamlColumnDefinition>>(yaml) ?? new();
            
            return Result.Ok<IReadOnlyList<YamlColumnDefinition>>(items);
        }
        catch (Exception ex)
        {
            return Result.Fail(new RecipeError($"Error parsing schema file '{schemaPath}': {ex.Message}", RecipeErrorCodes.ConfigParseError).CausedBy(ex));
        }
    }
}