using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NtoLib.OpcTreeManager.Entities;

public sealed record NodeSnapshot
{
	[JsonPropertyName("links")]
	public IReadOnlyList<LinkEntry> Links { get; init; } = Array.Empty<LinkEntry>();

	[JsonPropertyName("scadaItem")]
	public OpcScadaItemDto? ScadaItem { get; init; }
}
