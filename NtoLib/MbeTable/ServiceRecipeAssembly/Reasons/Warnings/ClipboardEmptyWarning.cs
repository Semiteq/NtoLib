using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Warnings;

public sealed class ClipboardEmptyWarning : BilingualWarning
{
	public ClipboardEmptyWarning()
		: base(
			"Clipboard is empty",
			"Буфер обмена пуст")
	{
	}
}
