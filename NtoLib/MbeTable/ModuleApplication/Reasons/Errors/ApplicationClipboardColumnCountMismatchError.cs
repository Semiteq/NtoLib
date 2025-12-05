using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationClipboardColumnCountMismatchError : BilingualError
{
	public int Actual { get; }
	public int Expected { get; }

	public ApplicationClipboardColumnCountMismatchError(int actual, int expected)
		: base(
			$"Clipboard column count mismatch: expected {expected}, got {actual}",
			$"Несоответствие количества столбцов в буфере: ожидается {expected}, получено {actual}")
	{
		Actual = actual;
		Expected = expected;
	}
}
