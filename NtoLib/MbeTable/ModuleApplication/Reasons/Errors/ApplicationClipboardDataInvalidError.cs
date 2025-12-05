using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationClipboardDataInvalidError : BilingualError
{
	public string Details { get; }

	public ApplicationClipboardDataInvalidError(string details)
		: base(
			$"Clipboard data is invalid: {details}",
			$"Данные в буфере обмена недействительны: {details}")
	{
		Details = details;
	}
}
