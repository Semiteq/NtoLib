using System;
using System.Collections.Generic;
using FluentResults;
using MasterSCADA.Hlp;
using Microsoft.Extensions.Logging;
using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Services;

public class ConfigLoaderReader
{
	private const int SourcesCount = 32;
	private const int ChamberHeatersCount = 16;
	private const int ShuttersCount = 16;

	private const string SourcesOutGroupName = "Sources_OUT";
	private const string ChamberHeatersOutGroupName = "ChamberHeaters_OUT";
	private const string ShuttersOutGroupName = "Shutters_OUT";

	private readonly IProjectHlp _project;
	private readonly ILogger<ConfigLoaderReader>? _logger;

	public ConfigLoaderReader(
		IProjectHlp project,
		ILoggerFactory? loggerFactory = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = loggerFactory?.CreateLogger<ConfigLoaderReader>();
	}

	public Result<Dictionary<ServiceType, string[]>> ReadOutputs(string configLoaderPath)
	{
		_logger?.LogInformation("Reading ConfigLoader outputs from path '{ConfigLoaderPath}'", configLoaderPath);

		if (string.IsNullOrWhiteSpace(configLoaderPath))
		{
			_logger?.LogWarning("ConfigLoader path is empty.");
			return Result.Fail("ConfigLoader path is empty");
		}

		var configLoader = _project.SafeItem<ITreeItemHlp>(configLoaderPath);
		if (configLoader == null)
		{
			_logger?.LogWarning("ConfigLoader not found at path '{ConfigLoaderPath}'", configLoaderPath);
			return Result.Fail($"ConfigLoader not found: {configLoaderPath}");
		}

		var result = new Dictionary<ServiceType, string[]>();

		var heatersResult = ReadPinGroup(configLoader, SourcesOutGroupName, SourcesCount);
		if (heatersResult.IsFailed)
		{
			_logger?.LogError("Failed to read pin group '{GroupName}' for heaters.", SourcesOutGroupName);
			return Result.Fail(heatersResult.Errors);
		}
		result[ServiceType.Heaters] = heatersResult.Value;

		var chamberHeatersResult = ReadPinGroup(configLoader, ChamberHeatersOutGroupName, ChamberHeatersCount);
		if (chamberHeatersResult.IsFailed)
		{
			_logger?.LogError("Failed to read pin group '{GroupName}' for chamber heaters.", ChamberHeatersOutGroupName);
			return Result.Fail(chamberHeatersResult.Errors);
		}
		result[ServiceType.ChamberHeaters] = chamberHeatersResult.Value;

		var shuttersResult = ReadPinGroup(configLoader, ShuttersOutGroupName, ShuttersCount);
		if (shuttersResult.IsFailed)
		{
			_logger?.LogError("Failed to read pin group '{GroupName}' for shutters.", ShuttersOutGroupName);
			return Result.Fail(shuttersResult.Errors);
		}
		result[ServiceType.Shutters] = shuttersResult.Value;

		_logger?.LogInformation(
			"ConfigLoader outputs read successfully. Heaters={HeatersCount}, ChamberHeaters={ChamberHeatersCount}, Shutters={ShuttersCount}",
			SourcesCount,
			ChamberHeatersCount,
			ShuttersCount);

		return Result.Ok(result);
	}

	private Result<string[]> ReadPinGroup(ITreeItemHlp configLoader, string groupName, int count)
	{
		_logger?.LogDebug(
			"Reading pin group '{GroupName}' with expected count {Count}.",
			groupName,
			count);

		var group = FindChildItem(configLoader, groupName);
		if (group == null)
		{
			_logger?.LogWarning("Pin group '{GroupName}' not found in ConfigLoader.", groupName);
			return Result.Fail<string[]>($"Group {groupName} not found in ConfigLoader");
		}

		var values = new string[count];

		for (var index = 0; index < count; index++)
		{
			var pinName = (index + 1).ToString();
			var pin = FindChildPin(group, pinName);

			values[index] = pin != null
				? GetPinStringValueOrEmpty(pin)
				: string.Empty;
		}

		return Result.Ok(values);
	}

	private static string GetPinStringValueOrEmpty(ITreePinHlp pin)
	{
		var pinValue = pin.GetConnectedRTPinValue();
		return pinValue?.Value?.ToString() ?? string.Empty;
	}

	private static ITreeItemHlp? FindChildItem(ITreeItemHlp parent, string name)
	{
		foreach (var child in parent.Childs)
		{
			if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return child as ITreeItemHlp;
			}
		}

		return null;
	}

	private static ITreePinHlp? FindChildPin(ITreeItemHlp parent, string name)
	{
		foreach (var child in parent.Childs)
		{
			if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return child as ITreePinHlp;
			}
		}

		return null;
	}
}
