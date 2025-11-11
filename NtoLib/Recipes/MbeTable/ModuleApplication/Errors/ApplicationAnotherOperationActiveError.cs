using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationAnotherOperationActiveError : BilingualError
{
    public ApplicationAnotherOperationActiveError()
        : base(
            "Another operation is already active",
            "Другая операция уже выполняется")
    {
    }
}