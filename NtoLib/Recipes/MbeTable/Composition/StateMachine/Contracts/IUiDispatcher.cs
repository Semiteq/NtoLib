using System;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.Contracts;

/// <summary>
/// Dispatches work to the UI thread.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Schedule the action to be executed on the UI thread asynchronously.
    /// </summary>
    void Post(Action action);
}