using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvFilePathEmptyError : BilingualError
{
	public CsvFilePathEmptyError()
		: base(
			"File path cannot be empty",
			"Путь к файлу не может быть пустым")
	{
	}
}
