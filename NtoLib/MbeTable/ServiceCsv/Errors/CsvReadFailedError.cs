using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvReadFailedError : BilingualError
{
	public string Details { get; }

	public CsvReadFailedError(string details)
		: base(
			$"Failed to read file: {details}",
			$"Не удалось прочитать файл: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}
}
