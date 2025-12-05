using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceCsv.Errors;

public sealed class CsvHeaderMismatchError : BilingualError
{
	public string[] Expected { get; }
	public string[] Actual { get; }

	public CsvHeaderMismatchError(string[] expected, string[] actual)
		: base(
			$"Header mismatch: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]",
			$"Заголовки не соответствуют: ожидалось [{string.Join(", ", expected)}], получено [{string.Join(", ", actual)}]")
	{
		Expected = expected;
		Actual = actual;
	}
}
