using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record TraversalData
{
	public TraversalData(IReadOnlyList<ChannelInfo> channels, IReadOnlyList<string> warnings)
	{
		Channels = channels ?? throw new ArgumentNullException(nameof(channels));
		Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
	}

	public IReadOnlyList<ChannelInfo> Channels { get; init; }
	public IReadOnlyList<string> Warnings { get; init; }
}
