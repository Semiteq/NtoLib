using System;

using FluentResults;

using NtoLib.ConfigLoader.Entities;
using NtoLib.ConfigLoader.Yaml;

namespace NtoLib.ConfigLoader.Io;

public class FileLoader
{
	private readonly object _fileLock;
	private readonly YamlDeserializer _yamlDeserializer = new();
	private readonly YamlValidator _validator;

	public FileLoader(object fileLock,
		uint shuttersQuantity,
		uint sourcesQuantity,
		uint chamberHeatersQuantity,
		uint waterChannelsQuantity)
	{
		_fileLock = fileLock ?? throw new ArgumentNullException(nameof(fileLock));
		_validator = new YamlValidator(
			shuttersQuantity,
			sourcesQuantity,
			chamberHeatersQuantity,
			waterChannelsQuantity);
	}

	public Result<LoaderDto> Load(string filePath)
	{
		Result<string> loadResult;
		lock (_fileLock)
		{
			loadResult = YamlLoader.Load(filePath);
		}

		return loadResult
			.Bind(yaml => _yamlDeserializer.Deserialize(yaml))
			.Bind(yamlConfig => _validator.ValidateAndMap(yamlConfig));
	}
}
