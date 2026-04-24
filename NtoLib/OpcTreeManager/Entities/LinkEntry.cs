using System.Text.Json.Serialization;

namespace NtoLib.OpcTreeManager.Entities;

/// <summary>
/// Represents one directed link: <see cref="LocalPinPath"/> ↔ <see cref="ExternalPinPath"/>.
/// <see cref="LocalPinPath"/> is the OPC-side pin; <see cref="ExternalPinPath"/> is the
/// consumer/producer pin outside the OPC subtree.
/// </summary>
public sealed record LinkEntry
{
	[JsonPropertyName("localPin")]
	public string LocalPinPath { get; init; } = string.Empty;

	[JsonPropertyName("externalPin")]
	public string ExternalPinPath { get; init; } = string.Empty;

	[JsonPropertyName("linkType")]
	public string LinkType { get; init; } = LinkTypes.DirectPin;
}
