using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvRecipeNullError : BilingualError
{
	public CsvRecipeNullError()
		: base(
			"Recipe cannot be null",
			"Рецепт не может быть null")
	{
	}
}
