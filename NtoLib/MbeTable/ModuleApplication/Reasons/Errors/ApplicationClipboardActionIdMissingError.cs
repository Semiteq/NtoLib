using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationClipboardActionIdMissingError : BilingualError
{
	public ApplicationClipboardActionIdMissingError()
		: base(
			"Action ID missing in clipboard row",
			"ID действия отсутствует в строке буфера обмена")
	{
	}
}
