using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Save;

public sealed class SaveRecipeOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.Save;
	public OperationKind Kind => OperationKind.Saving;
	public string DisplayNameRu => "сохранение рецепта";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Success;
	public bool IsLongRunning => true;
	public bool UpdatesPolicyReasons => false;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.None;
}
