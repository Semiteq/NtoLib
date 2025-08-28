#nullable enable

using NtoLib.Recipes.MbeTable.Composition.StateMachine.App;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.StateReducers
{
    /// <summary>
    /// Handles commands related to displaying and managing UI messages.
    /// </summary>
    internal sealed class MessageReducer : ICommandReducer
    {
        public AppState Reduce(AppState currentState, AppCommand command)
        {
            return command switch
            {
                // Sets the message in the state.
                PostMessage(var msg) => currentState with { Message = msg },

                // Clears the message only if it requires user acknowledgment.
                AckMessage when currentState.Message?.AckRequired == true => currentState with { Message = null },

                // Clears any message unconditionally.
                ClearMessage => currentState with { Message = null },

                // For any other command, do nothing.
                _ => currentState
            };
        }
    }
}