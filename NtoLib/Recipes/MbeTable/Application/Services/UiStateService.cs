using System;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Application.State;

namespace NtoLib.Recipes.MbeTable.Application.Services;

/// <summary>
/// Service managing UI state and permissions.
/// Wraps UiStateManager and provides application-level API.
/// </summary>
public sealed class UiStateService : IUiStateService
{
    private readonly UiStateManager _uiStateManager;
    private readonly ILogger _debugLogger;

    public event Action<UiPermissions>? PermissionsChanged
    {
        add => _uiStateManager.PermissionsChanged += value;
        remove => _uiStateManager.PermissionsChanged -= value;
    }

    public event Action<string, StatusKind>? StatusMessagePosted
    {
        add => _uiStateManager.MessagePosted += value;
        remove => _uiStateManager.MessagePosted -= value;
    }

    public UiStateService(UiStateManager uiStateManager, ILogger debugLogger)
    {
        _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    public UiPermissions GetCurrentPermissions()
    {
        var state = _uiStateManager.CurrentState;
        var isBusy = state.ActiveOperation != null || state.RecipeActive;

        _debugLogger.LogDebug(
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

    public void NotifyValidationChanged(bool isValid) => _uiStateManager.NotifyValidationChanged(isValid);
    public void NotifyPlcStateChanged(bool enaSendOk, bool recipeActive) => _uiStateManager.NotifyPlcStateChanged(enaSendOk, recipeActive);
    public void NotifyOperationStarted(OperationKind kind) => _uiStateManager.NotifyOperationStarted(kind);
    public void NotifyOperationCompleted() => _uiStateManager.NotifyOperationCompleted();
    public void ShowError(string message) => _uiStateManager.ShowError(message);
    public void ShowInfo(string message) => _uiStateManager.ShowInfo(message);
    public void ShowWarning(string message) => _uiStateManager.ShowWarning(message);
    public void ClearMessage() => _uiStateManager.ClearMessage();
}