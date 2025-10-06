

namespace NtoLib.Recipes.MbeTable.Application.State;

/// <summary>
/// Immutable snapshot of UI state used for permission calculation.
/// </summary>
public sealed record UiState(
    bool EnaSendOk,
    bool RecipeActive,
    OperationKind? ActiveOperation
)
{
    public static UiState Initial() => new(
        EnaSendOk: false,
        RecipeActive: false,
        ActiveOperation: null
    );

}