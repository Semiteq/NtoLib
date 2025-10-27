using System;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed class UiStateManager
{
    private readonly object _lock = new();
    private UiState _state = UiState.Initial();
    
    public event Action? StateChanged;

    public UiState CurrentState
    {
        get { lock (_lock) { return _state; } }
    }

    public void NotifyValidationChanged(bool isValid)
    {
        lock (_lock)
        {
            NotifyStateChanged();
        }
    }

    public void NotifyPlcStateChanged(bool enaSendOk, bool recipeActive)
    {
        lock (_lock)
        {
            if (_state.EnaSendOk == enaSendOk && _state.RecipeActive == recipeActive)
                return;

            _state = _state with { EnaSendOk = enaSendOk, RecipeActive = recipeActive };
            NotifyStateChanged();
        }
    }

    public void NotifyOperationStarted(OperationKind kind)
    {
        lock (_lock)
        {
            if (_state.ActiveOperation != null)
                return;

            _state = _state with { ActiveOperation = kind };
            NotifyStateChanged();
        }
    }

    public void NotifyOperationCompleted()
    {
        lock (_lock)
        {
            if (_state.ActiveOperation == null)
                return;

            _state = _state with { ActiveOperation = null };
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}