using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Load;

public sealed class LoadRecipeOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.Load;
	public OperationKind Kind => OperationKind.Loading;
	public string DisplayNameRu => "загрузка рецепта";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Success;
	public bool IsLongRunning => true;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
