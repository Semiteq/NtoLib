using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record AutoConfigResult(int ChannelsProcessed, int PensAdded, List<string> Warnings)
{
	public int ChannelsProcessed { get; init; } = ChannelsProcessed >= 0
		? ChannelsProcessed
		: throw new ArgumentOutOfRangeException(nameof(ChannelsProcessed));

	public int PensAdded { get; init; } = PensAdded >= 0
		? PensAdded
		: throw new ArgumentOutOfRangeException(nameof(PensAdded));
}
