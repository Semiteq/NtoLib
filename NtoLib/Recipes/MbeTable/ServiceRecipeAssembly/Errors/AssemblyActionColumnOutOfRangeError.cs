using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

public sealed class AssemblyActionColumnOutOfRangeError : BilingualError
{
    public AssemblyActionColumnOutOfRangeError()
        : base(
            "Action column not found or out of range",
            "Столбец действия не найден или вне диапазона")
    {
    }
}