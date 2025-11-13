using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationUnexpectedOperationError : BilingualError
{
    public ApplicationUnexpectedOperationError(string msg)
        : base(
            $"Unexpected error during operation {msg}",
            $"Непредвиденная ошибка при выполнении операции {msg}")
    {
    }
}