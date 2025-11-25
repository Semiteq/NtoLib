using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.AddStep;

public sealed class AddStepOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.AddStep;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "добавление строки";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => true;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.MarkInconsistent;
}
