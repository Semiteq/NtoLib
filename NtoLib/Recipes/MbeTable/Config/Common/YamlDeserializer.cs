

using System;
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Journaling.Errors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.Config.Common;

/// <summary>
/// YAML deserialization implementation using YamlDotNet.
/// </summary>
public sealed class YamlDeserializer : IYamlDeserializer
{
    private readonly IDeserializer _deserializer;

    public YamlDeserializer()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public Result<IReadOnlyList<T>> Deserialize<T>(string yaml) where T : class
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Result.Fail(new Error("YAML content is empty or null.")
                .WithMetadata("code", ErrorCode.ConfigInvalidSchema));
        }

        try
        {
            var items = _deserializer.Deserialize<List<T>>(yaml) ?? new List<T>();
            return Result.Ok<IReadOnlyList<T>>(items);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to deserialize YAML: {ex.Message}")
                .WithMetadata("code", ErrorCode.ConfigParseError)
                .CausedBy(ex));
        }
    }
}