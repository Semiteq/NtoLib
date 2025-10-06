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
    
    /// <inheritdoc />
    public event Action<UiPermissions>? PermissionsChanged;

    /// <inheritdoc />
    public event Action<string, StatusKind>? MessagePosted;

    /// <inheritdoc />
    public UiState CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public void NotifyValidationChanged(bool isValid)
    {
        lock (_lock)
        {
            RecalculatePermissions();
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void NotifyOperationStarted(OperationKind kind)
    {
        lock (_lock)
        {
            if (_state.ActiveOperation != null)
                return; // Already busy

            _state = _state with { ActiveOperation = kind };
            RecalculatePermissions();
        }
    }

    /// <inheritdoc />
    public void NotifyOperationCompleted()
    {
        lock (_lock)
        {
            if (_state.ActiveOperation == null)
                return; // Not busy

            _state = _state with { ActiveOperation = null };
            RecalculatePermissions();
        }
    }

    /// <inheritdoc />
    public void ShowError(string message)
    {
        MessagePosted?.Invoke(message, StatusKind.Error);
    }

    /// <inheritdoc />
    public void ShowInfo(string message)
    {
        MessagePosted?.Invoke(message, StatusKind.Info);
    }

    /// <inheritdoc />
    public void ShowWarning(string message)
    {
        MessagePosted?.Invoke(message, StatusKind.Warning);
    }

    /// <inheritdoc />
    public void ClearMessage()
    {
        MessagePosted?.Invoke(string.Empty, StatusKind.None);
    }

    /// <summary>
    /// Computes new permissions and raises PermissionsChanged if changed.
    /// Must be called within lock.
    /// </summary>
    private void RecalculatePermissions()
    {
        var isBusy = _state.ActiveOperation != null || _state.RecipeActive;

        var permissions = new UiPermissions(
            CanWriteRecipe: _state.EnaSendOk && !isBusy,
            CanOpenFile: !isBusy,
            CanAddStep: !isBusy,
            CanDeleteStep: !isBusy,
            CanSaveFile: !isBusy
        );
        
        PermissionsChanged?.Invoke(permissions);
    }
}