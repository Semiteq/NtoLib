using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreStepPropertyNotFoundError : BilingualError
{
	public CoreStepPropertyNotFoundError(string propertyKey, int rowIndex)
		: base(
			$"Property '{propertyKey}' not found in step at row {rowIndex}",
			$"Свойство '{propertyKey}' не найдено в шаге на строке {rowIndex + 1}")
	{
		PropertyKey = propertyKey;
		RowIndex = rowIndex;
	}

	public string PropertyKey { get; }
	public int RowIndex { get; }
}
