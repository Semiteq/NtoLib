using System;

namespace NtoLib.TrendPensManager.Entities;

public sealed record PenSequenceItem(string SourcePinPath, string TrendPath, string PenDisplayName)
{
	public string SourcePinPath { get; init; } = !string.IsNullOrWhiteSpace(SourcePinPath)
		? SourcePinPath
		: throw new ArgumentException(@"Source pin path must not be empty.", nameof(SourcePinPath));

	public string TrendPath { get; init; } = !string.IsNullOrWhiteSpace(TrendPath)
		? TrendPath
		: throw new ArgumentException(@"Trend path must not be empty.", nameof(TrendPath));

	public string PenDisplayName { get; init; } = !string.IsNullOrWhiteSpace(PenDisplayName)
		? PenDisplayName
		: throw new ArgumentException(@"Pen display name must not be empty.", nameof(PenDisplayName));
}
