using System;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Services;

/// <summary>
/// Service for managing UI state and permissions.
/// Provides API for querying current state and notifying about state changes.
/// </summary>
public interface IUiStateService
{
    /// <summary>
    /// Raised when UI permissions change.
    /// </summary>
    event Action<UiPermissions>? PermissionsChanged;

    /// <summary>
    /// Raised when a status message should be displayed.
    /// Parameters: message text, status kind.
    /// </summary>
    event Action<string, StatusKind>? StatusMessagePosted;

    /// <summary>
    /// Gets current UI permissions.
    /// </summary>
    /// <returns>Current permissions state.</returns>
    UiPermissions GetCurrentPermissions();

    /// <summary>
    /// Notifies that recipe validation state has changed.
    /// </summary>
    /// <param name="isValid">True if recipe is valid.</param>
    void NotifyValidationChanged(bool isValid);

    /// <summary>
    /// Notifies that PLC state has changed.
    /// </summary>
    /// <param name="enaSendOk">True if PLC is ready to receive recipes.</param>
    /// <param name="recipeActive">True if recipe is currently executing.</param>
    void NotifyPlcStateChanged(bool enaSendOk, bool recipeActive);

    /// <summary>
    /// Notifies that a long-running operation has started.
    /// </summary>
    /// <param name="kind">Type of operation.</param>
    void NotifyOperationStarted(OperationKind kind);

    /// <summary>
    /// Notifies that a long-running operation has completed.
    /// </summary>
    void NotifyOperationCompleted();

    /// <summary>
    /// Posts an error message to the UI.
    /// </summary>
    /// <param name="message">Error message text.</param>
    void ShowError(string message);

    /// <summary>
    /// Posts an informational message to the UI.
    /// </summary>
    /// <param name="message">Info message text.</param>
    void ShowInfo(string message);

    /// <summary>
    /// Posts a warning message to the UI.
    /// </summary>
    /// <param name="message">Warning message text.</param>
    void ShowWarning(string message);

    /// <summary>
    /// Clears the current status message.
    /// </summary>
    void ClearMessage();
}