using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ValidationTargetGroupNotAvailableError : BilingualError
{
    public int Row { get; }
    public short ActionId { get; }
    public string ActionName { get; }
    public string ColumnKey { get; }
    public string GroupName { get; }

    public ValidationTargetGroupNotAvailableError(
        int row,
        short actionId,
        string actionName,
        string columnKey,
        string groupName)
        : base(
            $"Row {row + 1}: actionId={actionId} ('{actionName}') column '{columnKey}' references group '{groupName}', which is not available",
            $"Строка {row + 1}: actionId={actionId} ('{actionName}') столбец '{columnKey}' ссылается на группу '{groupName}', которая недоступна")
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