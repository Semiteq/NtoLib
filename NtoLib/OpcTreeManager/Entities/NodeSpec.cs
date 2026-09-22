using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

/// <summary>
/// One entry in a target project's desired-state tree. A null <see cref="Children"/> keeps this node
/// and its whole current subtree; a non-null one, empty included, keeps only the listed children and
/// applies the same rule to each.
/// </summary>
public sealed record NodeSpec(string Name, IReadOnlyList<NodeSpec>? Children);
