using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

/// <summary>
/// A single entry in the desired-state tree for an OpcTreeManager target project.
/// A null <see cref="Children"/> means "leaf" — keep this node and its entire
/// current subtree untouched. A non-null <see cref="Children"/> (possibly empty)
/// means "keep this node, but within it keep only the listed children,
/// recursively applying the same rule".
/// </summary>
public sealed record NodeSpec(string Name, IReadOnlyList<NodeSpec>? Children);
