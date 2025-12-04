using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record ChannelInfo(
	string ServiceName,
	ServiceType ServiceType,
	int ChannelNumber,
	bool Used,
	List<ParameterInfo> Parameters)
{
	public string ServiceName { get; init; } = !string.IsNullOrWhiteSpace(ServiceName)
		? ServiceName
		: throw new ArgumentException(@"Service name must not be empty.", nameof(ServiceName));

	public int ChannelNumber { get; init; } = ChannelNumber >= 0
		? ChannelNumber
		: throw new ArgumentOutOfRangeException(nameof(ChannelNumber), @"Channel number must be greater than zero.");

	public List<ParameterInfo> Parameters { get; } = Parameters ?? throw new ArgumentNullException(nameof(Parameters));
}
