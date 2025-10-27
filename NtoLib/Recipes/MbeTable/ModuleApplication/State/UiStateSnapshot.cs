namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

// Immutable state snapshot for diagnostics and testing.
public sealed record UiStateSnapshot(
    bool IsValid,
    int StepCount,
    bool EnaSendOk,
    bool RecipeActive,
    OperationKind? ActiveOperation
);