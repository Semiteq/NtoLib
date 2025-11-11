using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceCsv.Errors;

public sealed class CsvRecipeNullError : BilingualError
{
    public CsvRecipeNullError()
        : base(
            "Recipe cannot be null",
            "Рецепт не может быть null")
    {
    }
}