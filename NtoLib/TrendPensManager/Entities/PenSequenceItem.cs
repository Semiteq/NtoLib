using System;

namespace NtoLib.TrendPensManager.Entities;

public sealed record PenSequenceItem
{
	public string SourcePinPath { get; init; }
	public string TrendPath { get; init; }
	public string PenDisplayName { get; init; }

	public PenSequenceItem(string sourcePinPath, string trendPath, string penDisplayName)
	{
		if (string.IsNullOrWhiteSpace(sourcePinPath))
		{
			throw new ArgumentException(@"Source pin path must not be empty.", nameof(sourcePinPath));
		}

		if (string.IsNullOrWhiteSpace(trendPath))
		{
			throw new ArgumentException(@"Trend path must not be empty.", nameof(trendPath));
		}

		if (string.IsNullOrWhiteSpace(penDisplayName))
		{
			throw new ArgumentException(@"Pen display name must not be empty.", nameof(penDisplayName));
		}

		SourcePinPath = sourcePinPath;
		TrendPath = trendPath;
		PenDisplayName = penDisplayName;
	}
}
