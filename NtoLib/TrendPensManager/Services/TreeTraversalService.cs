using System;
using System.Collections.Generic;
using FluentResults;
using MasterSCADA.Hlp;
using Microsoft.Extensions.Logging;
using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Services;

public class TreeTraversalService
{
	private const string UsedPinName = "Used";

	private readonly IProjectHlp _project;
	private readonly ILogger<TreeTraversalService>? _logger;

	public TreeTraversalService(
		IProjectHlp project,
		ILoggerFactory? loggerFactory = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = loggerFactory?.CreateLogger<TreeTraversalService>();
	}

	public Result<TraversalData> TraverseServices(string trendRootPath)
	{
		_logger?.LogInformation("Traversing services under trend root path '{TrendRootPath}'", trendRootPath);

		if (string.IsNullOrWhiteSpace(trendRootPath))
		{
			_logger?.LogWarning("Trend root path is empty while traversing services.");
			return Result.Fail("Trend root path is empty");
		}

		var trendRoot = _project.SafeItem<ITreeItemHlp>(trendRootPath);
		if (trendRoot == null)
		{
			_logger?.LogWarning("Trend root item not found for path '{TrendRootPath}'", trendRootPath);
			return Result.Fail($"Trend object not found: {trendRootPath}");
		}

		var channels = new List<ChannelInfo>();
		var warnings = new List<string>();

		foreach (var serviceItem in trendRoot.Childs)
		{
			if (serviceItem is not ITreeItemHlp service)
			{
				continue;
			}

			ProcessServiceItem(service, channels, warnings);
		}

		_logger?.LogInformation(
			"Traversal completed. Channels={ChannelsCount}, Warnings={WarningsCount}",
			channels.Count,
			warnings.Count);

		return Result.Ok(new TraversalData(channels, warnings));
	}

	private void ProcessServiceItem(
		ITreeItemHlp service,
		List<ChannelInfo> channels,
		List<string> warnings)
	{
		var serviceName = service.Name;
		var serviceType = ParseServiceType(serviceName);

		_logger?.LogDebug(
			"Processing service item. Name='{ServiceName}', Type={ServiceType}",
			serviceName,
			serviceType);

		foreach (var channelItem in service.Childs)
		{
			if (channelItem is not ITreeItemHlp channel)
			{
				continue;
			}

			if (!TryParseChannelNumber(channel.Name, out var channelNumber))
			{
				continue;
			}

			if (!TryReadUsedFlag(channel, out var used, warnings))
			{
				continue;
			}

			var parameters = CollectChannelParameters(channel);
			channels.Add(new ChannelInfo(serviceName, serviceType, channelNumber, used, parameters));

			_logger?.LogDebug(
				"Channel collected. Service='{ServiceName}', ChannelNumber={ChannelNumber}, Used={Used}, Parameters={ParametersCount}",
				serviceName,
				channelNumber,
				used,
				parameters.Count);
		}
	}

	private static bool TryParseChannelNumber(string channelName, out int channelNumber)
	{
		channelNumber = 0;

		if (string.IsNullOrEmpty(channelName))
		{
			return false;
		}

		// Find trailing digits
		var i = channelName.Length - 1;
		while (i >= 0 && char.IsDigit(channelName[i]))
		{
			i--;
		}

		// No digits at the end
		if (i == channelName.Length - 1)
		{
			return false;
		}

		var suffix = channelName.Substring(i + 1);
		return int.TryParse(suffix, out channelNumber) && channelNumber > 0;
	}

	private bool TryReadUsedFlag(
		ITreeItemHlp channel,
		out bool used,
		List<string> warnings)
	{
		used = false;

		var usedPin = FindPin(channel, UsedPinName);
		if (usedPin == null)
		{
			var message = $"Channel {channel.FullName} has no 'Used' pin, skipped";
			warnings.Add(message);
			_logger?.LogWarning(message);
			return false;
		}

		try
		{
			used = GetPinBoolValue(usedPin);
			return true;
		}
		catch (Exception ex)
		{
			var message = $"Failed to read 'Used' for {channel.FullName}, skipped";
			warnings.Add(message);
			_logger?.LogError(ex, "Failed to read 'Used' pin value for channel '{ChannelFullName}'", channel.FullName);
			return false;
		}
	}

	private static List<ParameterInfo> CollectChannelParameters(ITreeItemHlp channel)
	{
		var parameters = new List<ParameterInfo>();

		foreach (var child in channel.Childs)
		{
			if (child is ITreePinHlp pin &&
			    !string.Equals(pin.Name, UsedPinName, StringComparison.OrdinalIgnoreCase))
			{
				parameters.Add(new ParameterInfo(pin.Name, pin.FullName));
			}
		}

		return parameters;
	}

	private static ServiceType ParseServiceType(string serviceName)
	{
		return serviceName switch
		{
			"БКТ" => ServiceType.Heaters,
			"БП" => ServiceType.ChamberHeaters,
			"БУЗ" => ServiceType.Shutters,
			_ => ServiceType.Other
		};
	}

	private static ITreePinHlp? FindPin(ITreeItemHlp parent, string pinName)
	{
		foreach (var child in parent.Childs)
		{
			if (child is ITreePinHlp pin &&
			    string.Equals(pin.Name, pinName, StringComparison.OrdinalIgnoreCase))
			{
				return pin;
			}
		}

		return null;
	}

	private static bool GetPinBoolValue(ITreePinHlp pin)
	{
		var pinValue = pin.GetConnectedRTPinValue();
		if (pinValue == null)
		{
			return false;
		}

		var value = pinValue.Value;

		return value switch
		{
			bool b => b,
			int i => i != 0,
			double d => Math.Abs(d) > double.Epsilon,
			_ => Convert.ToBoolean(value)
		};
	}
}
