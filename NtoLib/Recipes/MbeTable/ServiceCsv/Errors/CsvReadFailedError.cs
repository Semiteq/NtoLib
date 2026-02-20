using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvReadFailedError : BilingualError
{
	public CsvReadFailedError(string details)
		: base(
			$"Failed to read file: {details}",
			$"Не удалось прочитать файл: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}

	public string Details { get; }
}
