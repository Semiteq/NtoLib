using System;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

/// <summary>
/// Service managing UI state and permissions.
/// Wraps UiStateManager and provides application-level API.
/// </summary>
public sealed class UiStateService : IUiStateService
{
    private readonly UiStateManager _uiStateManager;
    private readonly ILogger _debugLogger;
    private readonly IStatusService _status;

    public event Action<UiPermissions>? PermissionsChanged
    {
        add => _uiStateManager.PermissionsChanged += value;
        remove => _uiStateManager.PermissionsChanged -= value;
    }

    // Kept for compatibility, but not used in Label-sink model.
    public event Action<string, StatusKind>? StatusMessagePosted
    {
        add => _uiStateManager.MessagePosted += value;
        remove => _uiStateManager.MessagePosted -= value;
    }

    public UiStateService(UiStateManager uiStateManager, ILogger debugLogger, IStatusService status)
    {
        _uiStateManager = uiStateManager ?? throw new ArgumentNullException(nameof(uiStateManager));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _status = status ?? throw new ArgumentNullException(nameof(status));
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

    public void ShowError(string message) => _status.ShowError(message);
    public void ShowInfo(string message) => _status.ShowInfo(message);
    public void ShowWarning(string message) => _status.ShowWarning(message);
    public void ClearMessage() => _status.Clear();
}