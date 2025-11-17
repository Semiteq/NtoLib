using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Result of structure validation phase.
/// </summary>
public sealed record StructureResult(IReadOnlyList<IReason> Reasons);