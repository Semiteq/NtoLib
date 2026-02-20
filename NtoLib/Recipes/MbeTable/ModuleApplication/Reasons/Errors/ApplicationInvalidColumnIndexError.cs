using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationInvalidColumnIndexError : BilingualError
{
	public ApplicationInvalidColumnIndexError(int columnIndex)
		: base(
			$"Invalid column index: {columnIndex}",
			$"Недопустимый индекс столбца: {columnIndex}")
	{
		ColumnIndex = columnIndex;
		Metadata["columnIndex"] = columnIndex;
	}

	public int ColumnIndex { get; }
}
