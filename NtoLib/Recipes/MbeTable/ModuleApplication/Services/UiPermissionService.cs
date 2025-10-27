using System;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleCore;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

public sealed class UiPermissionService : IUiPermissionService
{
    private readonly UiStateManager _uiStateManager;
    private readonly IRecipeService _recipeService;

    public event Action<UiPermissions>? PermissionsChanged;

    public UiPermissionService(
        UiStateManager uiStateManager,
        IRecipeService recipeService)
    {
        _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _uiStateManager.StateChanged += OnStateChanged;
    }

    public UiPermissions GetCurrentPermissions()
    {
        var state = _uiStateManager.CurrentState;
        var isBusy = state.ActiveOperation != null || state.RecipeActive;
        var isValid = _recipeService.IsValid();

        var canSave = !isBusy && isValid;
        var canSend = state.EnaSendOk && !isBusy && isValid;

        return new UiPermissions(
            CanWriteRecipe: canSend,
            CanOpenFile: !isBusy,
            CanAddStep: !isBusy,
            CanDeleteStep: !isBusy,
            CanSaveFile: canSave,
            IsGridReadOnly: state.RecipeActive
        );
    }

    public void NotifyValidationChanged(bool isValid)
    {
        _uiStateManager.NotifyValidationChanged(isValid);
    }

    public void NotifyPlcStateChanged(bool enaSendOk, bool recipeActive)
    {
        _uiStateManager.NotifyPlcStateChanged(enaSendOk, recipeActive);
    }

    public void NotifyOperationStarted(OperationKind kind)
    {
        _uiStateManager.NotifyOperationStarted(kind);
    }

    public void NotifyOperationCompleted()
    {
        _uiStateManager.NotifyOperationCompleted();
    }

    private void OnStateChanged()
    {
        var permissions = GetCurrentPermissions();
        PermissionsChanged?.Invoke(permissions);
    }
}