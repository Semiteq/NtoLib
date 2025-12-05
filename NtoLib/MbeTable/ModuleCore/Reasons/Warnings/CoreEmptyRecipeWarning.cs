using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Warnings;

public sealed class CoreEmptyRecipeWarning : BilingualWarning
{
	public CoreEmptyRecipeWarning()
		: base(
			$"Recipe contains no rows",
			$"Рецепт не содержит строк")
	{
	}
}
