using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationClipboardRowEmptyError : BilingualError
{
	public ApplicationClipboardRowEmptyError()
		: base(
			"Clipboard row is empty",
			"Строка в буфере обмена пуста")
	{
	}
}
