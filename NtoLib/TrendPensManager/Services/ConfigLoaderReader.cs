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

	public Result<Dictionary<ServiceType, string[]>> ReadOutputs(
		string configLoaderRootPath,
		ServiceSelectionOptions? serviceSelectionOptions = null)
	{
		_logger?.LogInformation(
			"Reading ConfigLoader outputs from path '{ConfigLoaderRootPath}'",
			configLoaderRootPath);

		if (string.IsNullOrWhiteSpace(configLoaderRootPath))
		{
			_logger?.LogWarning("ConfigLoader root path is empty.");
			return Result.Fail("ConfigLoader root path is empty");
		}

		var configLoader = _project.SafeItem<ITreeItemHlp>(configLoaderRootPath);
		if (configLoader == null)
		{
			_logger?.LogWarning(
				"ConfigLoader not found at path '{ConfigLoaderRootPath}'",
				configLoaderRootPath);
			return Result.Fail($"ConfigLoader not found: {configLoaderRootPath}");
		}

		var result = new Dictionary<ServiceType, string[]>();

		if (serviceSelectionOptions == null || serviceSelectionOptions.AddHeaters)
		{
			var sourcesResult = ReadPinGroup(configLoader, SourcesOutGroupName, SourcesCount);
			if (sourcesResult.IsFailed)
			{
				_logger?.LogError(
					"Failed to read pin group '{GroupName}' for heaters.",
					SourcesOutGroupName);
				return Result.Fail(sourcesResult.Errors);
			}
			result[ServiceType.Heaters] = sourcesResult.Value;
		}
		else
		{
			_logger?.LogDebug("Heaters disabled, skipping {GroupName}", SourcesOutGroupName);
		}

		if (serviceSelectionOptions == null || serviceSelectionOptions.AddChamberHeaters)
		{
			var chamberHeatersResult = ReadPinGroup(configLoader, ChamberHeatersOutGroupName, ChamberHeatersCount);
			if (chamberHeatersResult.IsFailed)
			{
				_logger?.LogError(
					"Failed to read pin group '{GroupName}' for chamber heaters.",
					ChamberHeatersOutGroupName);
				return Result.Fail(chamberHeatersResult.Errors);
			}
			result[ServiceType.ChamberHeaters] = chamberHeatersResult.Value;
		}
		else
		{
			_logger?.LogDebug("Chamber heaters disabled, skipping {GroupName}", ChamberHeatersOutGroupName);
		}

		if (serviceSelectionOptions == null || serviceSelectionOptions.AddShutters)
		{
			var shuttersResult = ReadPinGroup(configLoader, ShuttersOutGroupName, ShuttersCount);
			if (shuttersResult.IsFailed)
			{
				_logger?.LogError(
					"Failed to read pin group '{GroupName}' for shutters.",
					ShuttersOutGroupName);
				return Result.Fail(shuttersResult.Errors);
			}
			result[ServiceType.Shutters] = shuttersResult.Value;
		}
		else
		{
			_logger?.LogDebug("Shutters disabled, skipping {GroupName}", ShuttersOutGroupName);
		}

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
			_logger?.LogWarning(
				"Pin group '{GroupName}' not found in ConfigLoader.",
				groupName);
			return Result.Fail<string[]>($"Group {groupName} not found in ConfigLoader");
		}

		var values = new string[count];
		var hasAnyValue = false;

		for (var index = 0; index < count; index++)
		{
			var pinName = (index + 1).ToString();
			var pin = FindChildPin(group, pinName);

			if (pin == null)
			{
				_logger?.LogError(
					"Pin '{PinName}' not found in group '{GroupName}' of ConfigLoader.",
					pinName,
					groupName);

				return Result.Fail<string[]>($"Pin {pinName} not found in group {groupName}");
			}

			var value = GetPinStringValueOrEmpty(pin);
			values[index] = value;

			if (!string.IsNullOrEmpty(value))
			{
				hasAnyValue = true;
			}
		}

		if (!hasAnyValue)
		{
			_logger?.LogError(
				"Pin group '{GroupName}' is empty in ConfigLoader.",
				groupName);
			return Result.Fail<string[]>($"Group {groupName} is empty in ConfigLoader");
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
