using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleApplication.Reasons.Warnings;

public sealed class ApplicationEmptyRecipeWarning : BilingualWarning
{
	public ApplicationEmptyRecipeWarning()
		: base(
			"Recipe is empty",
			"Рецепт пуст")
	{
	}
}
