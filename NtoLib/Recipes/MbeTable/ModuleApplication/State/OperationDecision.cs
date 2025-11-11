using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed record OperationDecision(
    DecisionKind Kind,
    IReason? PrimaryReason
)
{
    public static OperationDecision Allowed() =>
        new(DecisionKind.Allowed, null);

    public static OperationDecision BlockedWarning(IReason reason) =>
        new(DecisionKind.BlockedWarning, reason);

    public static OperationDecision BlockedError(IReason reason) =>
        new(DecisionKind.BlockedError, reason);
}