using System;

using FluentResults;

using NtoLib.ConfigLoader.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.ConfigLoader.Yaml;

public class YamlDeserializer
{
	private readonly IDeserializer _deserializer = new DeserializerBuilder()
		.WithNamingConvention(NullNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.Build();

	public Result<YamlConfigDto> Deserialize(string yaml)
	{
		if (string.IsNullOrWhiteSpace(yaml))
		{
			return Result.Fail("YAML content is empty or null.");
		}

		try
		{
			var raw = _deserializer.Deserialize<YamlConfigDto>(yaml);

			var yamlConfigDto = new YamlConfigDto(
				raw.Shutters,
				raw.Sources,
				raw.ChamberHeaters,
				raw.Waters,
				raw.Gases);

			return Result.Ok(yamlConfigDto);
		}
		catch (Exception ex)
		{
			return Result.Fail($"Failed to deserialize YAML: {ex.Message}");
		}
	}
}
