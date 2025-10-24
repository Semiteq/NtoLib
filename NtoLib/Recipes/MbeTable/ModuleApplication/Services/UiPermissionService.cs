using System;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

/// <summary>
/// Service managing UI state and permissions.
/// Wraps UiStateManager and provides application-level API.
/// </summary>
public sealed class UiPermissionService : IUiPermissionService
{
    private readonly UiStateManager _uiStateManager;
    private readonly ILogger<UiPermissionService> _logger;

    public event Action<UiPermissions>? PermissionsChanged
    {
        add => _uiStateManager.PermissionsChanged += value;
        remove => _uiStateManager.PermissionsChanged -= value;
    }

    public UiPermissionService(UiStateManager uiStateManager, ILogger<UiPermissionService> logger)
    {
        _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public UiPermissions GetCurrentPermissions()
    {
        var state = _uiStateManager.CurrentState;
        var isBusy = state.ActiveOperation != null || state.RecipeActive;

        _logger.LogDebug(
            "Calculating UI permissions: EnaSendOk={EnaSendOk}, IsBusy={IsBusy}, RecipeActive={RecipeActive}",
            state.EnaSendOk,
            isBusy,
            state.RecipeActive);

        return new UiPermissions(
            CanWriteRecipe: state.EnaSendOk && !isBusy,
            CanOpenFile: !isBusy,
            CanAddStep: !isBusy,
            CanDeleteStep: !isBusy,
            CanSaveFile: !isBusy,
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
}