using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreStepPropertyUpdateFailedError : BilingualError
{
    public int RowIndex { get; }
    public string ColumnName { get; }

    public CoreStepPropertyUpdateFailedError(int rowIndex, string columnName)
        : base(
            $"Failed to update property '{columnName}' at row {rowIndex}",
            $"Не удалось обновить свойство '{columnName}' на строке {rowIndex + 1}")
    {
        RowIndex = rowIndex;
        ColumnName = columnName;
    }
}