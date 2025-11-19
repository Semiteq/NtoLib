using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardRowEmptyError : BilingualError
{
    public int RowIndex { get; }

    public ClipboardRowEmptyError(int rowIndex)
        : base(
            $"Clipboard row {rowIndex} is empty",
            $"Строка буфера обмена {rowIndex} пуста")
    {
        RowIndex = rowIndex;
        Metadata["rowIndex"] = rowIndex;
    }
}