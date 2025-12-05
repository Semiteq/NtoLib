using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class ValidationTargetNotFoundError : BilingualError
{
	public int Row { get; }
	public short ActionId { get; }
	public string ActionName { get; }
	public string ColumnKey { get; }
	public short TargetId { get; }
	public string GroupName { get; }

	public ValidationTargetNotFoundError(
		int row,
		short actionId,
		string actionName,
		string columnKey,
		short targetId,
		string groupName)
		: base(
			$"Row {row + 1}: actionId={actionId} ('{actionName}') column '{columnKey}' targetId={targetId} not found in group '{groupName}'",
			$"Строка {row + 1}: actionId={actionId} ('{actionName}') столбец '{columnKey}' targetId={targetId} не найден в группе '{groupName}'")
	{
		Row = row;
		ActionId = actionId;
		ActionName = actionName;
		ColumnKey = columnKey;
		TargetId = targetId;
		GroupName = groupName;

		Metadata["row"] = row;
		Metadata["actionId"] = actionId;
		Metadata["actionName"] = actionName;
		Metadata["columnKey"] = columnKey;
		Metadata["targetId"] = targetId;
		Metadata["groupName"] = groupName;
	}
}
