using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreColumnNotFoundInActionError : BilingualError
{
    public string ActionName { get; }
    public short ActionId { get; }
    public string ColumnKey { get; }

    public CoreColumnNotFoundInActionError(string actionName, short actionId, string columnKey)
        : base(
            $"Action '{actionName}' (ID: {actionId}) does not contain column '{columnKey}'",
            $"Действие '{actionName}' (ID: {actionId}) не содержит столбец '{columnKey}'")
    {
        ActionName = actionName;
        ActionId = actionId;
        ColumnKey = columnKey;
    }
}