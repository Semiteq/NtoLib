using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreTargetsNotDefinedError : BilingualError
{
    public CoreTargetsNotDefinedError()
        : base(
            "No targets defined for group",
            "Не определены цели для группы")
    {
    }
}