using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CorePropertyNonNumericError : BilingualError
{
    public CorePropertyNonNumericError()
        : base(
            "Property holds a non-numeric string value",
            "Свойство содержит нечисловое строковое значение")
    {
    }
}