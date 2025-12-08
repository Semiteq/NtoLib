using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Send;

public sealed class SendRecipeOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.Send;
	public OperationKind Kind => OperationKind.Transferring;
	public string DisplayNameRu => "отправка рецепта";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Success;
	public bool IsLongRunning => true;
	public bool UpdatesPolicyReasons => false;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkConsistent;
}
