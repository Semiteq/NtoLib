using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CutSteps;

public sealed class CutRowsOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.CutRows;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "вырезание строк";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
