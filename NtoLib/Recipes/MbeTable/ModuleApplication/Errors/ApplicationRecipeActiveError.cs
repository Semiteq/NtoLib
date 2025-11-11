using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationRecipeActiveError : BilingualError
{
    public ApplicationRecipeActiveError()
        : base(
            "Operation not allowed while recipe is active",
            "Операция недоступна пока рецепт активен")
    {
    }
}