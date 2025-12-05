using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceClipboard.Reasons.Errors;

public sealed class ClipboardWriteError : BilingualError
{
	public string Details { get; }

	public ClipboardWriteError(string details)
		: base(
			$"Failed to write to clipboard: {details}",
			$"Не удалось записать в буфер обмена: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}
}
