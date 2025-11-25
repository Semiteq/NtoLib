using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvWriteFailedError : BilingualError
{
	public string Details { get; }

	public CsvWriteFailedError(string details)
		: base(
			$"Failed to write file: {details}",
			$"Не удалось записать файл: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}
}
