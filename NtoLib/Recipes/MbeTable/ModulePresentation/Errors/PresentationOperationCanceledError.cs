using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Errors;

public sealed class PresentationOperationCanceledError : BilingualError
{
    public PresentationOperationCanceledError()
        : base(
            "Operation canceled",
            "Операция отменена")
    {
    }
}