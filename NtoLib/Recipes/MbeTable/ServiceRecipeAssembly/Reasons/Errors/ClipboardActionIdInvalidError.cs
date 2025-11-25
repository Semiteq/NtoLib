using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardActionIdInvalidError : BilingualError
{
	public int RowIndex { get; }
	public string Value { get; }

	public ClipboardActionIdInvalidError(int rowIndex, string value)
		: base(
			$"Invalid action ID '{value}' at clipboard row {rowIndex}",
			$"Недопустимый ID действия '{value}' в строке буфера обмена {rowIndex}")
	{
		RowIndex = rowIndex;
		Value = value;
		Metadata["rowIndex"] = rowIndex;
		Metadata["value"] = value;
	}
}
