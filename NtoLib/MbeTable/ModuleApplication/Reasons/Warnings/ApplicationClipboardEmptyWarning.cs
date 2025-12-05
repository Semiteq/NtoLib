using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Warnings;

public sealed class ApplicationClipboardEmptyWarning : BilingualWarning
{
	public ApplicationClipboardEmptyWarning()
		: base(
			"Clipboard is empty",
			"Буфер обмена пуст")
	{
	}
}
