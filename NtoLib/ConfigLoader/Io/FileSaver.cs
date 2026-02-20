using System;
using System.Collections.Generic;
using System.IO;

using FluentResults;

using NtoLib.ConfigLoader.Entities;
using NtoLib.ConfigLoader.Yaml;

namespace NtoLib.ConfigLoader.Io;

public class FileSaver
{
	private readonly object _fileLock;
	private readonly YamlValidator _validator;
	private readonly YamlSerializer _yamlSerializer;

	public FileSaver(
		object fileLock,
		ConfigLoaderGroups groups)
	{
		_fileLock = fileLock ?? throw new ArgumentNullException(nameof(fileLock));

		if (groups == null)
		{
			throw new ArgumentNullException(nameof(groups));
		}

		_yamlSerializer = new YamlSerializer();
		_validator = new YamlValidator(
			groups.Shutters.Capacity,
			groups.Sources.Capacity,
			groups.ChamberHeaters.Capacity,
			groups.Water.Capacity,
			groups.Gases.Capacity);
	}

	public Result Save(string filePath, LoaderDto dto)
	{
		var validationResult = ValidateDto(dto);
		if (validationResult.IsFailed)
		{
			return Result.Fail(validationResult.Errors);
		}

		var yamlResult = _yamlSerializer.Serialize(dto);
		if (yamlResult.IsFailed)
		{
			return Result.Fail(yamlResult.Errors);
		}

		return WriteToFile(filePath, yamlResult.Value);
	}

	private Result<YamlConfigDto> ValidateDto(LoaderDto dto)
	{
		var yamlConfig = new YamlConfigDto(
			BuildDictionary(dto.Shutters),
			BuildDictionary(dto.Sources),
			BuildDictionary(dto.ChamberHeaters),
			BuildDictionary(dto.WaterChannels),
			BuildDictionary(dto.Gases));

		var validateResult = _validator.ValidateAndMap(yamlConfig);
		if (validateResult.IsFailed)
		{
			return Result.Fail(validateResult.Errors);
		}

		return Result.Ok(yamlConfig);
	}

	private Result WriteToFile(string filePath, string content)
	{
		try
		{
			lock (_fileLock)
			{
				var directory = Path.GetDirectoryName(filePath);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.WriteAllText(filePath, content);
			}
		}
		catch (Exception ex)
		{
			return Result.Fail($"Failed to write configuration file '{filePath}': {ex.Message}");
		}

		return Result.Ok();
	}

	private static Dictionary<string, string> BuildDictionary(string[] values)
	{
		var result = new Dictionary<string, string>();

		for (var i = 0; i < values.Length; i++)
		{
			var key = (i + 1).ToString();
			result[key] = values[i];
		}

		return result;
	}
}
