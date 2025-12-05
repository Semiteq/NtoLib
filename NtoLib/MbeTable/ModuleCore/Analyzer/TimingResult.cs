using System;
using System.Collections.Generic;

using NtoLib.MbeTable.ModuleCore.Loops;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Timing calculation outcome.
/// </summary>
public sealed record TimingResult(
	IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
	TimeSpan TotalDuration,
	IReadOnlyList<LoopNode> UpdatedNodes);
