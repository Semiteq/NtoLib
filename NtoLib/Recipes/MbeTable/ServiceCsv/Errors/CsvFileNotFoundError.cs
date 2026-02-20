using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvFileNotFoundError : BilingualError
{
	public CsvFileNotFoundError(string filePath)
		: base(
			$"File not found: {filePath}",
			$"Файл не найден: {filePath}")
	{
		FilePath = filePath;
		Metadata["filePath"] = filePath;
	}

	public string FilePath { get; }
}
