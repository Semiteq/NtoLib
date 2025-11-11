using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreActionNameEmptyError : BilingualError
{
    public CoreActionNameEmptyError()
        : base(
            "Action name is empty",
            "Имя действия пустое")
    {
    }
}