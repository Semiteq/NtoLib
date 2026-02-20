using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record ChannelInfo
{
	public ChannelInfo(
		string serviceName,
		ServiceType serviceType,
		int channelNumber,
		bool used,
		IReadOnlyList<ParameterInfo> parameters)
	{
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			throw new ArgumentException(@"Service name must not be empty.", nameof(serviceName));
		}

		if (channelNumber < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(channelNumber), @"Channel number must be greater than zero.");
		}

		ServiceName = serviceName;
		ServiceType = serviceType;
		ChannelNumber = channelNumber;
		Used = used;
		Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
	}

	public string ServiceName { get; init; }
	public ServiceType ServiceType { get; init; }
	public int ChannelNumber { get; init; }
	public bool Used { get; init; }
	public IReadOnlyList<ParameterInfo> Parameters { get; init; }
}
