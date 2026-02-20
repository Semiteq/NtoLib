using System;

using FluentResults;

using NtoLib.ConfigLoader.Entities;
using NtoLib.ConfigLoader.Yaml;

namespace NtoLib.ConfigLoader.Io;

public class FileLoader
{
	private readonly object _fileLock;
	private readonly YamlValidator _validator;
	private readonly YamlDeserializer _yamlDeserializer = new();

	public FileLoader(object fileLock, ConfigLoaderGroups groups)
	{
		_fileLock = fileLock ?? throw new ArgumentNullException(nameof(fileLock));

		if (groups == null)
		{
			throw new ArgumentNullException(nameof(groups));
		}

		_validator = new YamlValidator(
			groups.Shutters.Capacity,
			groups.Sources.Capacity,
			groups.ChamberHeaters.Capacity,
			groups.Water.Capacity,
			groups.Gases.Capacity);
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
