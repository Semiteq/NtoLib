using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Remove;

public sealed class RemoveStepOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.RemoveStep;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "удаление строки";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
