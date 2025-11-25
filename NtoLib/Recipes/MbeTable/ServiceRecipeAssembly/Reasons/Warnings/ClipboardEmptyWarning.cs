using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Warnings;

public sealed class ClipboardEmptyWarning : BilingualWarning
{
	public ClipboardEmptyWarning()
		: base(
			"Clipboard is empty",
			"Буфер обмена пуст")
	{
	}
}
