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
	private readonly ServiceFilter _serviceFilter;

	public TreeTraversalService(
		IProjectHlp project,
		ServiceFilter serviceFilter,
		ILoggerFactory? loggerFactory = null)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_serviceFilter = serviceFilter ?? throw new ArgumentNullException(nameof(serviceFilter));
		_logger = loggerFactory?.CreateLogger<TreeTraversalService>();
	}

	public Result<TraversalData> TraverseServices(
		string dataRootPath,
		ServiceSelectionOptions serviceSelectionOptions)
	{
		_ = serviceSelectionOptions ?? throw new ArgumentNullException(nameof(serviceSelectionOptions));

		_logger?.LogInformation(
			"Traversing services under data root path '{DataRootPath}'",
			dataRootPath);

		if (string.IsNullOrWhiteSpace(dataRootPath))
		{
			_logger?.LogWarning("Data root path is empty while traversing services.");
			return Result.Fail("Data root path is empty");
		}

		var dataRoot = _project.SafeItem<ITreeItemHlp>(dataRootPath);
		if (dataRoot == null)
		{
			_logger?.LogWarning("Data root item not found for path '{DataRootPath}'", dataRootPath);
			return Result.Fail($"Data root object not found: {dataRootPath}");
		}

		if (!serviceSelectionOptions.IsAnyServiceEnabled())
		{
			_logger?.LogInformation(
				"No services enabled in selection options. DataRootPath='{DataRootPath}'",
				dataRootPath);

			return Result.Ok(new TraversalData(new List<ChannelInfo>(), new List<string>()));
		}

		var channels = new List<ChannelInfo>();
		var warnings = new List<string>();

		foreach (var serviceItem in dataRoot.Childs)
		{
			if (serviceItem is not ITreeItemHlp serviceNode)
			{
				continue;
			}

			var serviceName = serviceNode.Name;
			if (!_serviceFilter.IsServiceEnabled(serviceName, serviceSelectionOptions))
			{
				_logger?.LogDebug(
					"Service '{ServiceName}' is disabled by selection options. Skipping.",
					serviceName);
				continue;
			}

			var serviceType = _serviceFilter.GetServiceType(serviceName);

			var serviceResult = ProcessServiceItem(serviceNode, serviceName, serviceType, channels, warnings);
			if (serviceResult.IsFailed)
			{
				return Result.Fail<TraversalData>(serviceResult.Errors);
			}
		}

		_logger?.LogInformation(
			"Traversal completed. Channels={ChannelsCount}, Warnings={WarningsCount}",
			channels.Count,
			warnings.Count);

		return Result.Ok(new TraversalData(channels, warnings));
	}

	private Result ProcessServiceItem(
		ITreeItemHlp service,
		string serviceName,
		ServiceType serviceType,
		List<ChannelInfo> channels,
		List<string> warnings)
	{
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

			var usedResult = ReadUsedFlag(channel);
			if (usedResult.IsFailed)
			{
				return usedResult.ToResult();
			}

			var used = usedResult.Value;
			var parameters = CollectChannelParameters(channel);
			channels.Add(new ChannelInfo(serviceName, serviceType, channelNumber, used, parameters));

			_logger?.LogDebug(
				"Channel collected. Service='{ServiceName}', ChannelNumber={ChannelNumber}, Used={Used}, Parameters={ParametersCount}",
				serviceName,
				channelNumber,
				used,
				parameters.Count);
		}

		return Result.Ok();
	}

	private static bool TryParseChannelNumber(string channelName, out int channelNumber)
	{
		channelNumber = 0;

		if (string.IsNullOrEmpty(channelName))
		{
			return false;
		}

		var index = channelName.Length - 1;
		while (index >= 0 && char.IsDigit(channelName[index]))
		{
			index--;
		}

		if (index == channelName.Length - 1)
		{
			return false;
		}

		var suffix = channelName.Substring(index + 1);
		return int.TryParse(suffix, out channelNumber) && channelNumber > 0;
	}

	private Result<bool> ReadUsedFlag(ITreeItemHlp channel)
	{
		var usedPin = FindPin(channel, UsedPinName);
		if (usedPin == null)
		{
			var message = $"Channel {channel.FullName} has no '{UsedPinName}' pin.";
			_logger?.LogError(message);
			return Result.Fail<bool>(message);
		}

		try
		{
			var used = GetPinBoolValue(usedPin);
			return Result.Ok(used);
		}
		catch (Exception ex)
		{
			var message = $"Failed to read '{UsedPinName}' for {channel.FullName}.";
			_logger?.LogError(
				ex,
				"Failed to read 'Used' pin value for channel '{ChannelFullName}'",
				channel.FullName);
			return Result.Fail<bool>(message);
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
