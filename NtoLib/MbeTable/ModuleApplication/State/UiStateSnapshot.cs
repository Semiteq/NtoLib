using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;

namespace NtoLib.MbeTable.ModuleApplication.State;

// Immutable state snapshot for diagnostics and testing.
public sealed record UiStateSnapshot(
	int StepCount,
	bool EnaSendOk,
	bool RecipeActive,
	bool IsRecipeConsistent,
	OperationKind? ActiveOperation
);
