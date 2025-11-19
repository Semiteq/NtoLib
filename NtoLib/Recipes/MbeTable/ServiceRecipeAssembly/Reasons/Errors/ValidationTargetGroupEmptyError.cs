using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ValidationTargetGroupEmptyError : BilingualError
{
    public int Row { get; }
    public short ActionId { get; }
    public string ActionName { get; }
    public string ColumnKey { get; }
    public string GroupName { get; }

    public ValidationTargetGroupEmptyError(
        int row,
        short actionId,
        string actionName,
        string columnKey,
        string groupName)
        : base(
            $"Row {row + 1}: actionId={actionId} ('{actionName}') column '{columnKey}' group '{groupName}' has no targets configured",
            $"Строка {row + 1}: actionId={actionId} ('{actionName}') столбец '{columnKey}' группа '{groupName}' не содержит настроенных целей")
    {
        Row = row;
        ActionId = actionId;
        ActionName = actionName;
        ColumnKey = columnKey;
        GroupName = groupName;
        
        Metadata["row"] = row;
        Metadata["actionId"] = actionId;
        Metadata["actionName"] = actionName;
        Metadata["columnKey"] = columnKey;
        Metadata["groupName"] = groupName;
    }
}