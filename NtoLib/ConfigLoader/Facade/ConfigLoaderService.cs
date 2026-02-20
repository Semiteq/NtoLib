using System;

using FluentResults;

using NtoLib.ConfigLoader.Entities;
using NtoLib.ConfigLoader.Io;
using NtoLib.ConfigLoader.Io.Default;
using NtoLib.ConfigLoader.Yaml;

namespace NtoLib.ConfigLoader.Facade;

public class ConfigLoaderService : IConfigLoaderService
{
	private readonly FileLoader _fileLoader;
	private readonly FileSaver _fileSaver;
	private readonly ConfigLoaderGroups _groups;

	public ConfigLoaderService(
		object fileLock,
		ConfigLoaderGroups groups)
	{
		var fileLock1 = fileLock ?? throw new ArgumentNullException(nameof(fileLock));
		_groups = groups ?? throw new ArgumentNullException(nameof(groups));

		_fileLoader = new FileLoader(fileLock1, _groups);
		_fileSaver = new FileSaver(fileLock1, _groups);

		CurrentConfiguration = DefaultConfigurationFactory.Create(_groups);

		IsLoaded = false;
		LastError = string.Empty;
	}

	public LoaderDto CurrentConfiguration { get; private set; }
	public bool IsLoaded { get; private set; }
	public string LastError { get; private set; }

	public Result<LoaderDto> Load(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return HandleError("File path cannot be empty.");
		}

		if (!YamlLoader.FileExists(filePath))
		{
			var createResult = CreateDefaultFile(filePath);
			if (createResult.IsFailed)
			{
				return HandleError(FormatErrors(createResult));
			}
		}

		var result = _fileLoader.Load(filePath);

		if (result.IsFailed)
		{
			return HandleError(result);
		}

		CurrentConfiguration = result.Value;
		IsLoaded = true;
		LastError = string.Empty;

		return Result.Ok(CurrentConfiguration);
	}

	public Result Save(string filePath, LoaderDto dto)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			LastError = "File path cannot be empty.";

			return Result.Fail(LastError);
		}

		var saveResult = _fileSaver.Save(filePath, dto);

		if (saveResult.IsFailed)
		{
			LastError = FormatErrors(saveResult);

			return Result.Fail(LastError);
		}

		return Result.Ok();
	}

	public Result SaveAndReload(string filePath, LoaderDto dto)
	{
		var saveResult = Save(filePath, dto);

		if (saveResult.IsFailed)
		{
			return saveResult;
		}

		var loadResult = Load(filePath);

		if (loadResult.IsFailed)
		{
			return Result.Fail(loadResult.Errors);
		}

		return Result.Ok();
	}

	public LoaderDto CreateEmptyConfiguration()
	{
		return DefaultConfigurationFactory.Create(_groups);
	}

	private Result CreateDefaultFile(string filePath)
	{
		var defaultDto = DefaultConfigurationFactory.Create(_groups);

		return _fileSaver.Save(filePath, defaultDto);
	}

	private Result<LoaderDto> HandleError(string message)
	{
		IsLoaded = false;
		LastError = message;

		return Result.Fail(message);
	}

	private Result<LoaderDto> HandleError(Result<LoaderDto> failedResult)
	{
		IsLoaded = false;
		LastError = FormatErrors(failedResult);

		return Result.Fail(failedResult.Errors);
	}

	private static string FormatErrors(ResultBase result)
	{
		if (result.IsSuccess)
		{
			return string.Empty;
		}

		return string.Join(" | ", result.Errors);
	}
}
