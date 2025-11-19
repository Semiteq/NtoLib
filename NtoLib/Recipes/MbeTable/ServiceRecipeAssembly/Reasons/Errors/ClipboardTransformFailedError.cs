using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ClipboardTransformFailedError : BilingualError
{
    public int RowIndex { get; }
    public string Details { get; }

    public ClipboardTransformFailedError(int rowIndex, string details)
        : base(
            $"Failed to transform clipboard row {rowIndex}: {details}",
            $"Не удалось преобразовать строку буфера обмена {rowIndex}: {details}")
    {
        RowIndex = rowIndex;
        Details = details;
        Metadata["rowIndex"] = rowIndex;
        Metadata["details"] = details;
    }
}