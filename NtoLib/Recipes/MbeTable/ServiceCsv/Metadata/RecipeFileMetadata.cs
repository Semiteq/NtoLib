using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

public sealed record RecipeFileMetadata
{
	public char Separator { get; init; } = ';';

	public int Rows { get; init; } = 0;

	public string BodyHashBase64 { get; init; } = "";

	public Dictionary<string, string> Extras { get; init; } = new();
}
