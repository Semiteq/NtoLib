using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ValidationTargetValueEmptyError : BilingualError
{
    public int Row { get; }
    public short ActionId { get; }
    public string ActionName { get; }
    public string ColumnKey { get; }
    public string GroupName { get; }

    public ValidationTargetValueEmptyError(
        int row,
        short actionId,
        string actionName,
        string columnKey,
        string groupName)
        : base(
            $"Row {row + 1}: actionId={actionId} ('{actionName}') column '{columnKey}' requires a target from group '{groupName}', but value is empty",
            $"Строка {row + 1}: actionId={actionId} ('{actionName}') столбец '{columnKey}' требует цель из группы '{groupName}', но значение пустое")
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