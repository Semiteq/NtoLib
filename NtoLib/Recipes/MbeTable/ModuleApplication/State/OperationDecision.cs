using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

// Result of evaluating an operation against current policy.
public sealed record OperationDecision(
    DecisionKind Kind,
    Codes PrimaryCode,
    IReadOnlyList<Codes> AllCodes
)
{
    public static OperationDecision Allowed() =>
        new(DecisionKind.Allowed, Codes.UnknownError, new List<Codes>());

    public static OperationDecision BlockedWarning(Codes code) =>
        new(DecisionKind.BlockedWarning, code, new List<Codes> { code });

    public static OperationDecision BlockedError(Codes code) =>
        new(DecisionKind.BlockedError, code, new List<Codes> { code });
}