using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationValidationFailedError : BilingualError
{
    public string Reason { get; }

    public ApplicationValidationFailedError(string reason)
        : base(
            $"Validation failed: {reason}",
            $"Проверка не пройдена: {reason}")
    {
        Reason = reason;
        Metadata["reason"] = reason;
    }
}