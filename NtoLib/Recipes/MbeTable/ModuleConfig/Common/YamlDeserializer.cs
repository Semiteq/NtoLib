using System;
using System.Collections.Generic;

using FluentResults;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// YAML deserialization using YamlDotNet. Produces Result with ConfigError on failure.
/// </summary>
public sealed class YamlDeserializer : IYamlDeserializer
{
	private readonly IDeserializer _deserializer;

	public YamlDeserializer()
	{
		_deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.IgnoreUnmatchedProperties() // Ignore extra properties in YAML (Non-strict)
			.Build();
	}

	public Result<IReadOnlyList<T>> Deserialize<T>(string yaml) where T : class
	{
		if (string.IsNullOrWhiteSpace(yaml))
		{
			return Result.Fail(new ConfigError(
				"YAML content is empty or null.",
				section: "YAML",
				context: "deserialization"));
		}

		try
		{
			var items = _deserializer.Deserialize<List<T>>(yaml) ?? new List<T>();
			return Result.Ok<IReadOnlyList<T>>(items);
		}
		catch (Exception ex)
		{
			return Result.Fail(new ConfigError(
				$"Failed to deserialize YAML: {ex.Message}",
				section: "YAML",
				context: "deserialization",
				cause: ex));
		}
	}
}
