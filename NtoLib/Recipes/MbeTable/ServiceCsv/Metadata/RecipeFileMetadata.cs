using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

/// <summary>
/// Represents metadata for a recipe file, including signature, version information,
/// separator details, row count, body hash, and any extra data.
/// </summary>
public sealed record RecipeFileMetadata
{
	/// <summary>
	/// CSV separator (always ';')
	/// </summary>
	public char Separator { get; init; } = ';';

	/// <summary>
	/// Total number of data rows
	/// </summary>
	public int Rows { get; init; } = 0;

	/// <summary>
	/// SHA-256 hash of normalized data rows
	/// </summary>
	public string BodyHashBase64 { get; init; } = "";

	/// <summary>
	/// Additional metadata for extensibility
	/// </summary>
	public Dictionary<string, string> Extras { get; init; } = new();
}
