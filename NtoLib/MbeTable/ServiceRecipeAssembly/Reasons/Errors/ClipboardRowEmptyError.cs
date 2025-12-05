using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardRowEmptyError : BilingualError
{
	public int RowIndex { get; }

	public ClipboardRowEmptyError(int rowIndex)
		: base(
			$"Clipboard row {rowIndex} is empty",
			$"Строка буфера обмена {rowIndex} пуста")
	{
		RowIndex = rowIndex;
		Metadata["rowIndex"] = rowIndex;
	}
}
