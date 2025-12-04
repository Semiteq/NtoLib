using System;
using System.Collections.Generic;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Services;

public class PenSequenceBuilder
{
	private readonly ILogger<PenSequenceBuilder>? _logger;

	public PenSequenceBuilder(ILoggerFactory? loggerFactory = null)
	{
		_logger = loggerFactory?.CreateLogger<PenSequenceBuilder>();
	}

	public Result<PenSequenceData> BuildSequence(
		List<ChannelInfo> channels,
		Dictionary<ServiceType, string[]> configNames,
		string trendPath)
	{
		_logger?.LogInformation(
			"Building pen sequence. Channels={ChannelsCount}, TrendPath='{TrendPath}'",
			channels?.Count ?? 0,
			trendPath);

		if (channels == null)
		{
			throw new ArgumentNullException(nameof(channels));
		}

		if (configNames == null)
		{
			throw new ArgumentNullException(nameof(configNames));
		}

		if (string.IsNullOrWhiteSpace(trendPath))
		{
			_logger?.LogWarning("Trend path is empty while building pen sequence.");
			return Result.Fail("Trend path is empty");
		}

		var sequenceItems = new List<PenSequenceItem>();
		var warnings = new List<string>();

		if (channels.Count == 0)
		{
			_logger?.LogInformation("No channels provided for pen sequence. Returning empty sequence.");
			return Result.Ok(new PenSequenceData(sequenceItems, warnings));
		}

		foreach (var channel in channels)
		{
			if (!channel.Used)
			{
				_logger?.LogDebug(
					"Skipping channel because it is not used. Service='{ServiceName}', ChannelNumber={ChannelNumber}",
					channel.ServiceName,
					channel.ChannelNumber);

				continue;
			}

			var configNameResult = ResolveConfigName(channel, configNames);
			if (configNameResult.IsFailed)
			{
				return Result.Fail<PenSequenceData>(configNameResult.Errors);
			}

			var configName = configNameResult.Value;

			foreach (var parameter in channel.Parameters)
			{
				var displayName = FormatPenName(parameter.Name, configName);
				sequenceItems.Add(new PenSequenceItem(parameter.FullPath, trendPath, displayName));

				_logger?.LogDebug(
					"Pen added to sequence. Service='{ServiceName}', ChannelNumber={ChannelNumber}, Param='{ParamName}', DisplayName='{DisplayName}'",
					channel.ServiceName,
					channel.ChannelNumber,
					parameter.Name,
					displayName);
			}
		}

		_logger?.LogInformation(
			"Pen sequence built. PensInSequence={PensCount}, Warnings={WarningsCount}",
			sequenceItems.Count,
			warnings.Count);

		return Result.Ok(new PenSequenceData(sequenceItems, warnings));
	}

	private Result<string?> ResolveConfigName(
		ChannelInfo channel,
		Dictionary<ServiceType, string[]> configNames)
	{
		if (channel.ServiceType == ServiceType.Other)
		{
			_logger?.LogDebug(
				"Service type '{ServiceType}' does not use ConfigLoader names. Service='{ServiceName}', ChannelNumber={ChannelNumber}",
				channel.ServiceType,
				channel.ServiceName,
				channel.ChannelNumber);

			return Result.Ok<string?>(null);
		}

		if (!configNames.TryGetValue(channel.ServiceType, out var names))
		{
			var message = $"No ConfigLoader data for service type {channel.ServiceType}";
			_logger?.LogError(
				"No ConfigLoader data for service type '{ServiceType}'.",
				channel.ServiceType);

			return Result.Fail<string?>(message);
		}

		var index = channel.ChannelNumber - 1;
		if (index < 0 || index >= names.Length)
		{
			var message = $"Channel index {channel.ChannelNumber} is out of bounds for service type {channel.ServiceType}";
			_logger?.LogError(
				"Channel index {ChannelNumber} is out of bounds for service type '{ServiceType}'. ArrayLength={Length}",
				channel.ChannelNumber,
				channel.ServiceType,
				names.Length);

			return Result.Fail<string?>(message);
		}

		var name = names[index];
		_logger?.LogDebug(
			"Config name resolved for service type '{ServiceType}', channel {ChannelNumber}: '{ConfigName}'",
			channel.ServiceType,
			channel.ChannelNumber,
			name);

		return Result.Ok<string?>(name);
	}

	private static string FormatPenName(string paramName, string? configName)
	{
		if (string.IsNullOrWhiteSpace(configName))
		{
			return paramName;
		}

		return $"{paramName} {configName}";
	}
}
