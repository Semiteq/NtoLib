using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreRecipeStepsNullError : BilingualError
{
	public CoreRecipeStepsNullError()
		: base(
			"Recipe.Steps is zero length or null",
			"В рецепте нету строк")
	{
	}
}
