using System;
using System.Collections.Generic;

namespace NtoLib.TrendPensManager.Entities;

public sealed record PenSequenceData
{
	public IReadOnlyList<PenSequenceItem> Sequence { get; init; }
	public IReadOnlyList<string> Warnings { get; init; }

	public PenSequenceData(IReadOnlyList<PenSequenceItem> sequence, IReadOnlyList<string> warnings)
	{
		Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
		Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
	}
}
