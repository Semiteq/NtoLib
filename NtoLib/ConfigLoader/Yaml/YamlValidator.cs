using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using FluentResults;

using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Yaml;

public class YamlValidator
{
	private readonly uint _shutterCapacity;
	private readonly uint _sourceCapacity;
	private readonly uint _chamberHeaterCapacity;
	private readonly uint _waterCapacity;

	private static readonly Regex _allowedNamePattern = new(
		@"^[A-Za-zА-Яа-яЁё0-9 ._\-]*$",
		RegexOptions.Compiled);

	private const int MaxNameLength = 255;

	public YamlValidator(
		uint shutterCapacity,
		uint sourceCapacity,
		uint chamberHeaterCapacity,
		uint waterCapacity)
	{
		_shutterCapacity = shutterCapacity;
		_sourceCapacity = sourceCapacity;
		_chamberHeaterCapacity = chamberHeaterCapacity;
		_waterCapacity = waterCapacity;
	}

	public Result<LoaderDto> ValidateAndMap(YamlConfigDto yamlDto)
	{
		var shutterResult = ValidateAndBuildArray(yamlDto.Shutter, _shutterCapacity, "Shutter");
		var sourcesResult = ValidateAndBuildArray(yamlDto.Sources, _sourceCapacity, "Source");
		var chamberHeaterResult = ValidateAndBuildArray(yamlDto.ChamberHeater, _chamberHeaterCapacity, "ChamberHeater");
		var waterResult = ValidateAndBuildArray(yamlDto.Water, _waterCapacity, "Water");

		var combinedResult = Result.Merge(
			shutterResult,
			sourcesResult,
			chamberHeaterResult,
			waterResult);

		if (combinedResult.IsFailed)
		{
			return Result.Fail(combinedResult.Errors);
		}

		var dto = new LoaderDto(
			shutterResult.Value,
			sourcesResult.Value,
			chamberHeaterResult.Value,
			waterResult.Value);

		return Result.Ok(dto);
	}

	private Result<string[]> ValidateAndBuildArray(
		Dictionary<string, string> groupValues,
		uint capacity,
		string groupName)
	{
		if (capacity == 0)
		{
			return Result.Ok(Array.Empty<string>());
		}

		if (groupValues == null)
		{
			return Result.Fail($"Group '{groupName}' is missing.");
		}

		var parsedKeys = new Dictionary<int, string>();

		foreach (var kvp in groupValues)
		{
			if (!int.TryParse(kvp.Key, out var intKey))
			{
				return Result.Fail($"Group '{groupName}' has non-integer key '{kvp.Key}'.");
			}

			if (intKey < 1 || intKey > capacity)
			{
				return Result.Fail($"Group '{groupName}' has out-of-range key '{intKey}'.  Expected 1 to {capacity}.");
			}

			if (parsedKeys.ContainsKey(intKey))
			{
				return Result.Fail($"Group '{groupName}' has duplicate key '{intKey}'.");
			}

			parsedKeys[intKey] = kvp.Value ?? string.Empty;
		}

		for (var expectedKey = 1; expectedKey <= capacity; expectedKey++)
		{
			if (!parsedKeys.ContainsKey(expectedKey))
			{
				return Result.Fail($"Group '{groupName}' is missing key '{expectedKey}'.");
			}
		}

		if (parsedKeys.Count != capacity)
		{
			return Result.Fail(
				$"Group '{groupName}' has invalid key count. Expected {capacity}, actual {parsedKeys.Count}.");
		}

		var result = new string[capacity];

		for (var index = 1; index <= capacity; index++)
		{
			var value = parsedKeys[index];

			var nameValidation = ValidateName(value, groupName, index);
			if (nameValidation.IsFailed)
			{
				return Result.Fail(nameValidation.Errors);
			}

			result[index - 1] = value;
		}

		return Result.Ok(result);
	}

	private Result ValidateName(string value, string groupName, int index)
	{
		if (value.Length >= MaxNameLength)
		{
			return Result.Fail(
				$"Name in group '{groupName}' at index '{index}' exceeds maximum length of {MaxNameLength}.");
		}

		if (!_allowedNamePattern.IsMatch(value))
		{
			return Result.Fail($"Name in group '{groupName}' at index '{index}' contains invalid characters.");
		}

		return Result.Ok();
	}
}
