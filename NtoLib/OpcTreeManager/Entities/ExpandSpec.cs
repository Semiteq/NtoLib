using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

public sealed record ExpandSpec(
	string Name,
	OpcScadaItemDto ScadaItem,
	IReadOnlyList<LinkEntry> Links);
