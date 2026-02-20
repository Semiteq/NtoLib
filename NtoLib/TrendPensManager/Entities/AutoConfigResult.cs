using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record AutoConfigResult
{
	public AutoConfigResult(int channelsProcessed, int pensAdded, IReadOnlyList<string> warnings)
	{
		if (channelsProcessed < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(channelsProcessed));
		}

		if (pensAdded < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pensAdded));
		}

		ChannelsProcessed = channelsProcessed;
		PensAdded = pensAdded;
		Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
	}

	public int ChannelsProcessed { get; init; }
	public int PensAdded { get; init; }
	public IReadOnlyList<string> Warnings { get; init; }
}
