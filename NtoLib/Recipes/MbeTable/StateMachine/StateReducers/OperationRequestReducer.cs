#nullable enable
using System;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.StateMachine.App;

namespace NtoLib.Recipes.MbeTable.StateMachine.StateReducers
{
    /// <summary>
    /// Handles requests to start long-running operations like loading or saving recipes.
    /// </summary>
    internal sealed class OperationRequestReducer : ICommandReducer
    {
        private readonly ILogger _debugLogger;

        public OperationRequestReducer(ILogger debugLogger)
        {
            _debugLogger = debugLogger;
        }

        public AppState Reduce(AppState currentState, AppCommand command)
        {
            if (!CanStartOperation(currentState))
            {
                _debugLogger.Log("Operation denied: another one is already running");
                return currentState with
                {
                    Message = new UiMessage(MessageTag.None, "Операция уже выполняется.", StatusKind.Warning, false, false)
                };
            }

            var opId = Guid.NewGuid();
            return command switch
            {
                LoadRecipeRequested(var path) => currentState with { Busy = BusyKind.Loading, ActiveOperationId = opId, ActiveFilePath = path },
                SaveRecipeRequested(var path) => currentState with { Busy = BusyKind.Saving, ActiveOperationId = opId, ActiveFilePath = path },
                SendRecipeRequested => currentState with { Busy = BusyKind.Transferring, ActiveOperationId = opId, ActiveFilePath = null },
                ReadRecipeRequested => currentState with { Busy = BusyKind.Transferring, ActiveOperationId = opId, ActiveFilePath = null },
                _ => currentState
            };
        }

        private static bool CanStartOperation(AppState s)
        {
            return s.Busy is BusyKind.Idle or BusyKind.Executing && s.ActiveOperationId is null;
        }
    }
}