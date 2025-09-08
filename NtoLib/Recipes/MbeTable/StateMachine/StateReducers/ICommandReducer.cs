#nullable enable

using NtoLib.Recipes.MbeTable.StateMachine.App;

namespace NtoLib.Recipes.MbeTable.StateMachine.StateReducers
{
    /// <summary>
    /// Defines a strategy for reducing a state based on a specific command.
    /// </summary>
    internal interface ICommandReducer
    {
        /// <summary>
        /// Applies a command to the current state to produce a new state.
        /// </summary>
        /// <param name="currentState">The current application state.</param>
        /// <param name="command">The command to process.</param>
        /// <returns>The new application state.</returns>
        AppState Reduce(AppState currentState, AppCommand command);
    }
}