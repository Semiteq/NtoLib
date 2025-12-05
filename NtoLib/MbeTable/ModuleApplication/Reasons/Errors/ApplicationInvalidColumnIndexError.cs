using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationInvalidColumnIndexError : BilingualError
{
	public int ColumnIndex { get; }

	public ApplicationInvalidColumnIndexError(int columnIndex)
		: base(
			$"Invalid column index: {columnIndex}",
			$"Недопустимый индекс столбца: {columnIndex}")
	{
		ColumnIndex = columnIndex;
		Metadata["columnIndex"] = columnIndex;
	}
}
