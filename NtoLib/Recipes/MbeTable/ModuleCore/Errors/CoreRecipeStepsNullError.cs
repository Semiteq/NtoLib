using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreRecipeStepsNullError : BilingualError
{
    public CoreRecipeStepsNullError()
        : base(
            "Recipe.Steps is zero length or null",
            "В рецепте нету строк")
    {
    }
}