using System.Collections.Generic;

namespace NtoLib.LinkSwitcher.Entities;

public sealed record SwitchPlan(
	IReadOnlyList<ObjectPair> Pairs,
	IReadOnlyList<LinkOperation> Operations,
	bool Forward);
