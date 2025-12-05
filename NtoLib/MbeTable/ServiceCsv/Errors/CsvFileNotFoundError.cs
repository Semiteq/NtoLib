using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvFileNotFoundError : BilingualError
{
	public string FilePath { get; }

	public CsvFileNotFoundError(string filePath)
		: base(
			$"File not found: {filePath}",
			$"Файл не найден: {filePath}")
	{
		FilePath = filePath;
		Metadata["filePath"] = filePath;
	}
}
