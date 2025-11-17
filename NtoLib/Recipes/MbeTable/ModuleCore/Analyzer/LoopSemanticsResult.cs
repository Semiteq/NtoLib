using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleCore.Loops;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Loop semantics after resolving iteration counts, depths, and integrity flags.
/// </summary>
public sealed record LoopSemanticsResult(
    IReadOnlyList<LoopNode> Nodes,
    IReadOnlyList<IReason> Reasons,
    bool LoopIntegrityCompromised,
    bool MaxDepthExceeded);