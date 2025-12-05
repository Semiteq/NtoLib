using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationInvalidRowIndexError : BilingualError
{
	public int RowIndex { get; }

	public ApplicationInvalidRowIndexError(int rowIndex)
		: base(
			$"Invalid row index: {rowIndex}",
			$"Недопустимый индекс строки: {rowIndex}")
	{
		RowIndex = rowIndex;
		Metadata["rowIndex"] = rowIndex;
	}
}
