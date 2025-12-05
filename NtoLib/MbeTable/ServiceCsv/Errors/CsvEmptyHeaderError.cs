using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvEmptyHeaderError : BilingualError
{
	public CsvEmptyHeaderError()
		: base(
			"CSV header is empty",
			"Заголовок CSV пуст")
	{
	}
}
