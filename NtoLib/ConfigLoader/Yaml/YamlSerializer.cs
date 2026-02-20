using System.Collections.Generic;

using FluentResults;

using NtoLib.ConfigLoader.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.ConfigLoader.Yaml;

public class YamlSerializer
{
	private readonly ISerializer _serializer;

	public YamlSerializer()
	{
		_serializer = new SerializerBuilder()
			.WithNamingConvention(NullNamingConvention.Instance)
			.WithEventEmitter(next => new QuotedStringEmitter(next))
			.Build();
	}

	public Result<string> Serialize(LoaderDto dto)
	{
		var shutter = BuildDictionary(dto.Shutters);
		var source = BuildDictionary(dto.Sources);
		var chamberHeater = BuildDictionary(dto.ChamberHeaters);
		var water = BuildDictionary(dto.WaterChannels);
		var gases = BuildDictionary(dto.Gases);

		var yamlConfig = new YamlConfigDto(shutter, source, chamberHeater, water, gases);

		var yaml = _serializer.Serialize(yamlConfig);

		return Result.Ok(yaml);
	}

	private static Dictionary<string, string> BuildDictionary(string[] values)
	{
		var result = new Dictionary<string, string>();

		for (var index = 0; index < values.Length; index++)
		{
			var key = (index + 1).ToString();
			result[key] = values[index];
		}

		return result;
	}
}
