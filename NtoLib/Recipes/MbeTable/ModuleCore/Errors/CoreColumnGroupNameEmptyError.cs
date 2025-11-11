using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreColumnGroupNameEmptyError : BilingualError
{
    public CoreColumnGroupNameEmptyError()
        : base(
            "Column GroupName is empty",
            "GroupName столбца пуст")
    {
    }
}