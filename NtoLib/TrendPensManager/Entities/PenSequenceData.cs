using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record PenSequenceData(List<PenSequenceItem> Sequence, List<string> Warnings)
{
	public List<PenSequenceItem> Sequence { get; } = Sequence ?? throw new ArgumentNullException(nameof(Sequence));
	public List<string> Warnings { get; } = Warnings ?? throw new ArgumentNullException(nameof(Warnings));
}
