using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardActionIdMissingError : BilingualError
{
	public ClipboardActionIdMissingError(int rowIndex)
		: base(
			$"Action ID missing at clipboard row {rowIndex}",
			$"ID действия отсутствует в строке буфера обмена {rowIndex}")
	{
		RowIndex = rowIndex;
		Metadata["rowIndex"] = rowIndex;
	}

	public int RowIndex { get; }
}
