using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.EditCell;

public sealed class EditCellArgs
{
    public int RowIndex { get; }
    public ColumnIdentifier ColumnKey { get; }
    public object Value { get; }

    public EditCellArgs(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        RowIndex = rowIndex;
        ColumnKey = columnKey;
        Value = value;
    }
}