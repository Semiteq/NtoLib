using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvEmptyHeaderError : BilingualError
{
	public CsvEmptyHeaderError()
		: base(
			"CSV header is empty",
			"Заголовок CSV пуст")
	{
	}
}
