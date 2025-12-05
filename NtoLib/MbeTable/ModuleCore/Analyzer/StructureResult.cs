using System.Collections.Generic;

using FluentResults;

namespace NtoLib.MbeTable.ModuleCore.Analyzer;

/// <summary>
/// Result of structure validation phase.
/// </summary>
public sealed record StructureResult(IReadOnlyList<IReason> Reasons);
