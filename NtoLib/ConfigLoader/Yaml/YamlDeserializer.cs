using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.ConfigLoader.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.ConfigLoader.Yaml;

public class YamlDeserializer
{
	private readonly IDeserializer _deserializer;

	public YamlDeserializer()
	{
		_deserializer = new DeserializerBuilder()
			.WithNamingConvention(NullNamingConvention.Instance)
			.IgnoreUnmatchedProperties()
			.Build();
	}

	public Result<YamlConfigDto> Deserialize(string yaml)
	{
		if (string.IsNullOrWhiteSpace(yaml))
		{
			return Result.Fail("YAML content is empty or null.");
		}

		try
		{
			var raw = _deserializer.Deserialize<YamlConfigDto>(yaml);

			if (raw == null)
			{
				return Result.Fail("YAML content could not be deserialized to configuration.");
			}

			var shutter = raw.Shutter ?? new Dictionary<string, string>();
			var source = raw.Sources ?? new Dictionary<string, string>();
			var chamberHeater = raw.ChamberHeater ?? new Dictionary<string, string>();
			var water = raw.Water ?? new Dictionary<string, string>();

			return Result.Ok(new YamlConfigDto(shutter, source, chamberHeater, water));
		}
		catch (Exception ex)
		{
			return Result.Fail($"Failed to deserialize YAML: {ex.Message}");
		}
	}
}
