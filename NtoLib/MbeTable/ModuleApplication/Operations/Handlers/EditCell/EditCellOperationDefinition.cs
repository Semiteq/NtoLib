using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.EditCell;

public sealed class EditCellOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.EditCell;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "обновление ячейки";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.None;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
