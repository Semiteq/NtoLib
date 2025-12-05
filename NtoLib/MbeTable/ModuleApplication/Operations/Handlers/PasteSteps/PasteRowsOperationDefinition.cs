using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.PasteSteps;

public sealed class PasteRowsOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.PasteRows;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "вставка строк";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
