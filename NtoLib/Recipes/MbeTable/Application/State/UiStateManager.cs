using System;

namespace NtoLib.Recipes.MbeTable.Application.State;

/// <summary>
/// Manages UI state and computes button permissions based on input signals.
/// Thread-safe for state updates; events are raised synchronously.

/// </summary>
public sealed class UiStateManager
{
    private readonly object _lock = new();
    private UiState _state = UiState.Initial();
    
    public event Action<UiPermissions>? PermissionsChanged;
    public event Action<string, StatusKind>? MessagePosted;

    public UiState CurrentState
    {
        get { lock (_lock) { return _state; } }
    }

    public void NotifyValidationChanged(bool isValid)
    {
        lock (_lock)
        {
            RecalculatePermissions();
        }
    }

    public void NotifyPlcStateChanged(bool enaSendOk, bool recipeActive)
    {
        lock (_lock)
        {
            if (_state.EnaSendOk == enaSendOk && _state.RecipeActive == recipeActive)
                return;

            _state = _state with { EnaSendOk = enaSendOk, RecipeActive = recipeActive };
            RecalculatePermissions();
        }
    }

    public void NotifyOperationStarted(OperationKind kind)
    {
        lock (_lock)
        {
            if (_state.ActiveOperation != null)
                return;

            _state = _state with { ActiveOperation = kind };
            RecalculatePermissions();
        }
    }

    public void NotifyOperationCompleted()
    {
        lock (_lock)
        {
            if (_state.ActiveOperation == null)
                return;

            _state = _state with { ActiveOperation = null };
            RecalculatePermissions();
        }
    }

    public void ShowError(string message)   => MessagePosted?.Invoke(message, StatusKind.Error);
    public void ShowInfo(string message)    => MessagePosted?.Invoke(message, StatusKind.Info);
    public void ShowWarning(string message) => MessagePosted?.Invoke(message, StatusKind.Warning);
    public void ClearMessage()              => MessagePosted?.Invoke(string.Empty, StatusKind.None);

    private void RecalculatePermissions()
    {
        var isBusy = _state.ActiveOperation != null || _state.RecipeActive;

        var permissions = new UiPermissions(
            CanWriteRecipe: _state.EnaSendOk && !isBusy,
            CanOpenFile: !isBusy,
            CanAddStep: !isBusy,
            CanDeleteStep: !isBusy,
            CanSaveFile: !isBusy,
            IsGridReadOnly: _state.RecipeActive
        );

        PermissionsChanged?.Invoke(permissions);
    }
}