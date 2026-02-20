using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed record OperationDecision(
	DecisionKind Kind,
	IReason? PrimaryReason
)
{
	public static OperationDecision Allowed()
	{
		return new(DecisionKind.Allowed, null);
	}

	public static OperationDecision BlockedWarning(IReason reason)
	{
		return new(DecisionKind.BlockedWarning, reason);
	}

	public static OperationDecision BlockedError(IReason reason)
	{
		return new(DecisionKind.BlockedError, reason);
	}
}
