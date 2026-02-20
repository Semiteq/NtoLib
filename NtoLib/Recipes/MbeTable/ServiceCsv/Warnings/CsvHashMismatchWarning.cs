using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Warnings;

public sealed class CsvHashMismatchWarning : BilingualWarning
{
	public CsvHashMismatchWarning() : base(
		$"CSV file was modified externally. Hash didn't match.",
		$"CSV файл был изменен вне приложения. Хэш не совпал.")
	{
	}
}
