using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Errors;

public sealed class ApplicationInvalidColumnIndexError : BilingualError
{
    public int ColumnIndex { get; }

    public ApplicationInvalidColumnIndexError(int columnIndex)
        : base(
            $"Invalid column index: {columnIndex}",
            $"Недопустимый индекс столбца: {columnIndex}")
    {
        ColumnIndex = columnIndex;
        Metadata["columnIndex"] = columnIndex;
    }
}