using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.Operations;

public interface IOperationDefinition
{
	OperationId Id { get; }
	OperationKind Kind { get; }
	string DisplayNameRu { get; }
	CompletionMessageKind CompletionMessage { get; }
	bool IsLongRunning { get; }
	bool UpdatesPolicyReasons { get; }
	ConsistencyEffect ConsistencyEffect { get; }
}
