using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ServiceCsv.Metadata;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Data;

public sealed class CsvRawData
{
	public IReadOnlyList<string> Headers { get; set; } = new List<string>();

	public IReadOnlyList<string> Rows { get; set; } = new List<string>();

	public IReadOnlyList<string[]> Records { get; set; } = new List<string[]>();

	public RecipeFileMetadata? Metadata { get; set; }
}
