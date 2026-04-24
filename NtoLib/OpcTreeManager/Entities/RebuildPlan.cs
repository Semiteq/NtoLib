using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

public sealed record RebuildPlan(
	string OpcFbPath,
	string GroupName,
	IReadOnlyList<string> DesiredNodeNames,
	IReadOnlyList<ExpandSpec> ExpandSpecs);
