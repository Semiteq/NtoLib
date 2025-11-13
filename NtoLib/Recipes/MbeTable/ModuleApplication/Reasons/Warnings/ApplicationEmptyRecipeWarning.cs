using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Warnings;

public sealed class ApplicationEmptyRecipeWarning : BilingualWarning
{
    public ApplicationEmptyRecipeWarning()
        : base(
            "Recipe is empty",
            "Рецепт пуст")
    {
    }
}