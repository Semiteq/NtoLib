using System;
using System.IO;

using FluentResults;

namespace NtoLib.ConfigLoader.Yaml;

public static class YamlLoader
{
	public static Result<string> Load(string filePath)
	{
		return ValidatePath(filePath)
			.Bind(ValidateFileExists)
			.Bind(ReadFileContent);
	}

	public static bool FileExists(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return false;
		}

		return File.Exists(filePath);
	}

	private static Result<string> ValidatePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return Result.Fail("File path cannot be empty.");
		}

		return Result.Ok(path);
	}

	private static Result<string> ValidateFileExists(string path)
	{
		if (!File.Exists(path))
		{
			return Result.Fail($"Configuration file not found at: '{path}'");
		}

		return Result.Ok(path);
	}

	private static Result<string> ReadFileContent(string path)
	{
		try
		{
			var content = File.ReadAllText(path);
			return Result.Ok(content);
		}
		catch (Exception ex)
		{
			return Result.Fail($"Failed to read file '{path}': {ex.Message}");
		}
	}
}
