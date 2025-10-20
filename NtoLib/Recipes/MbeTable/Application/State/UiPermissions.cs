

namespace NtoLib.Recipes.MbeTable.Application.State;

/// <summary>
/// Represents computed UI permissions based on current application state.
/// </summary>
public sealed record UiPermissions(
    bool CanWriteRecipe,
    bool CanOpenFile,
    bool CanAddStep,
    bool CanDeleteStep,
    bool CanSaveFile,
    bool IsGridReadOnly
);