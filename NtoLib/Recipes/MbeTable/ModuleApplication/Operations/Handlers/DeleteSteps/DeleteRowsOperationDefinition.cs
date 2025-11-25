using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.DeleteSteps;

public sealed class DeleteRowsOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.DeleteRows;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "удаление нескольких строк";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
