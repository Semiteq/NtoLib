using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationUnexpectedOperationError : BilingualError
{
    public ApplicationUnexpectedOperationError()
        : base(
            "Unexpected error during operation",
            "Непредвиденная ошибка при выполнении операции")
    {
    }
}