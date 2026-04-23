using System;
using System.IO;

using FluentResults;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NtoLib.OpcTreeManager.Config;

public static class OpcConfigLoader
{
	public static Result<OpcConfig> Load(string path)
	{
		if (!File.Exists(path))
		{
			return Result.Fail($"Config file not found: {path}");
		}

		return Result.Try(
			() => Deserialize(path),
			ex => new Error($"Error reading config file '{path}': {ex.Message}"));
	}

	private static OpcConfig Deserialize(string path)
	{
		var yaml = File.ReadAllText(path);

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		return deserializer.Deserialize<OpcConfig>(yaml)
			?? throw new InvalidOperationException($"Config file parsed as null: {path}");
	}
}
