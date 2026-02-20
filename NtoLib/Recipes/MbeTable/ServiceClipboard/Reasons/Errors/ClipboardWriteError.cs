using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceClipboard.Reasons.Errors;

public sealed class ClipboardWriteError : BilingualError
{
	public ClipboardWriteError(string details)
		: base(
			$"Failed to write to clipboard: {details}",
			$"Не удалось записать в буфер обмена: {details}")
	{
		Details = details;
		Metadata["details"] = details;
	}

	public string Details { get; }
}
