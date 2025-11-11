using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Errors;

public sealed class PresentationParameterRequiredError : BilingualError
{
    public string ParameterName { get; }

    public PresentationParameterRequiredError(string parameterName)
        : base(
            $"{parameterName} is required",
            $"{parameterName} обязателен")
    {
        ParameterName = parameterName;
        Metadata["parameterName"] = parameterName;
    }
}