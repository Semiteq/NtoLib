using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record TraversalData(List<ChannelInfo> Channels, List<string> Warnings)
{
	public List<ChannelInfo> Channels { get; } = Channels ?? throw new ArgumentNullException(nameof(Channels));
	public List<string> Warnings { get; } = Warnings ?? throw new ArgumentNullException(nameof(Warnings));
}
