using System.Collections.Generic;

namespace NtoLib.LinkSwitcher.Entities;

public sealed record SwitchPlan(
	IReadOnlyList<PairOperations> PairResults,
	bool Reverse,
	string SourcePath,
	string TargetPath);
