using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationUnexpectedIoWriteError : BilingualError
{
    public ApplicationUnexpectedIoWriteError()
        : base(
            "Unexpected error during write operation",
            "Непредвиденная ошибка при записи")
    {
    }
}