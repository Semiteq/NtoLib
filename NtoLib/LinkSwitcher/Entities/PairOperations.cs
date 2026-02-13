using System.Collections.Generic;

namespace NtoLib.LinkSwitcher.Entities;

public sealed record PairOperations(
	ObjectPair Pair,
	IReadOnlyList<LinkOperation> Operations,
	IReadOnlyList<string> StructureWarnings);
