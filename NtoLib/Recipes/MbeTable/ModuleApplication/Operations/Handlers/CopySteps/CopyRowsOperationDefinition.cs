using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.CopySteps;

public sealed class CopyRowsOperationDefinition : IOperationDefinition
{
	public OperationId Id => OperationId.CopyRows;
	public OperationKind Kind => OperationKind.Other;
	public string DisplayNameRu => "копирование строк";
	public CompletionMessageKind CompletionMessage => CompletionMessageKind.Info;
	public bool IsLongRunning => false;
	public bool UpdatesPolicyReasons => false;
	public ConsistencyEffect ConsistencyEffect => ConsistencyEffect.None;
}
