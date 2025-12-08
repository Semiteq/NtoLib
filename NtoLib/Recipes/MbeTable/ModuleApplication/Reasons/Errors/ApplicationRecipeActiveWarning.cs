using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationRecipeActiveWarning : BilingualError
{
	public ApplicationRecipeActiveWarning()
		: base(
			"Operation not allowed while recipe is active",
			"Операция недоступна пока рецепт активен")
	{
	}
}
