using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardColumnCountMismatchError : BilingualError
{
	public int Actual { get; }
	public int Expected { get; }
	public int RowIndex { get; }

	public ClipboardColumnCountMismatchError(int rowIndex, int actual, int expected)
		: base(
			$"Clipboard row {rowIndex} has {actual} columns, expected {expected}",
			$"Строка буфера обмена {rowIndex} содержит {actual} столбцов, ожидалось {expected}")
	{
		RowIndex = rowIndex;
		Actual = actual;
		Expected = expected;
		Metadata["rowIndex"] = rowIndex;
		Metadata["actual"] = actual;
		Metadata["expected"] = expected;
	}
}
