using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyPlcIndexOutOfRangeError : BilingualError
{
    public int Index { get; }
    public int Row { get; }
    public string ColumnKey { get; }
    public string Area { get; }

    public AssemblyPlcIndexOutOfRangeError(int index, int row, string columnKey, string area)
        : base(
            $"{area} index {index} out of range for row {row}, column '{columnKey}'",
            $"Индекс {area} {index} вне диапазона для строки {row}, столбец '{columnKey}'")
    {
        Index = index;
        Row = row;
        ColumnKey = columnKey;
        Area = area;
        Metadata["index"] = index;
        Metadata["row"] = row;
        Metadata["columnKey"] = columnKey;
        Metadata["area"] = area;
    }
}