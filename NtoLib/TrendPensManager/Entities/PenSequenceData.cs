using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record PenSequenceData(List<PenSequenceItem> Plan, List<string> Warnings)
{
	public List<PenSequenceItem> Plan { get; } = Plan ?? throw new ArgumentNullException(nameof(Plan));
	public List<string> Warnings { get; } = Warnings ?? throw new ArgumentNullException(nameof(Warnings));
}
