using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationInvalidOperationError : BilingualError
{
    public string Details { get; }

    public ApplicationInvalidOperationError(string details)
        : base(
            $"Invalid operation: {details}",
            $"Недопустимая операция: {details}")
    {
        Details = details;
        Metadata["details"] = details;
    }
}