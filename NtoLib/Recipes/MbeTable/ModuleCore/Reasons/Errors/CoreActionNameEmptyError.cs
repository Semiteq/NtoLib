using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreActionNameEmptyError : BilingualError
{
    public CoreActionNameEmptyError()
        : base(
            "Action name is empty",
            "Имя действия пустое")
    {
    }
}