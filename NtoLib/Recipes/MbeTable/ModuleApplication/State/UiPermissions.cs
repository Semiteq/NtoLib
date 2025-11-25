namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

/// <summary>
/// Represents computed UI permissions based on the current application state.
/// </summary>
public sealed record UiPermissions(
	bool CanSendRecipe,
	bool CanOpenFile,
	bool CanAddStep,
	bool CanDeleteStep,
	bool CanSaveFile,
	bool IsGridReadOnly
);
