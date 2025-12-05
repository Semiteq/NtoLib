using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Warnings;

public sealed class CsvHashMismatchWarning : BilingualWarning
{
	public string ExpectedHash { get; }
	public string ActualHash { get; }

	public CsvHashMismatchWarning(string expectedHash, string actualHash)
		: base(
			$"CSV file was modified externally. Expected hash: {expectedHash}, actual: {actualHash}",
			$"CSV файл был изменен вне приложения. Ожидаемый хеш: {expectedHash}, фактический: {actualHash}")
	{
		ExpectedHash = expectedHash;
		ActualHash = actualHash;
	}
}
