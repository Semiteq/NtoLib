using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Recive;

public sealed class ReceiveRecipeOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.Receive;
	public OperationKind Kind => OperationKind.Transferring;
	public string DisplayNameRu => "чтение рецепта";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Success;
	public bool IsLongRunning => true;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkConsistent;
}
