using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Loops;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Raw loop parse output before semantic normalization.
/// </summary>
public sealed record LoopParseResult(
	IReadOnlyList<LoopNode> Nodes,
	IReadOnlyList<IReason> Reasons);
