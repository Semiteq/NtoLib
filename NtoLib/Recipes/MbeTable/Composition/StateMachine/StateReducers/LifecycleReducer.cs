#nullable enable
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using System;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.StateReducers
{
    /// <summary>
    /// Handles lifecycle commands like entering editor or runtime mode.
    /// </summary>
    internal sealed class LifecycleReducer : ICommandReducer
    {
        private readonly DebugLogger _debugLogger;

        public LifecycleReducer(DebugLogger debugLogger)
        {
            _debugLogger = debugLogger;
        }

        public AppState Reduce(AppState currentState, AppCommand command)
        {
            return command switch
            {
                EnterEditor => currentState with { Mode = AppMode.Editor },
                EnterRuntime => StartRuntimeMode(currentState),
                _ => currentState
            };
        }

        private AppState StartRuntimeMode(AppState state)
        {
            var busy = state.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring
                ? state.Busy
                : (state.RecipeActive ? BusyKind.Executing : BusyKind.Idle);

            var nextState = state with { Mode = AppMode.Runtime, Busy = busy };
            
            return MaybeStartAutoRead(nextState);
        }
        
        private AppState MaybeStartAutoRead(AppState state)
        {
            if (!ShouldAutoRead(state)) return state;

            var opId = Guid.NewGuid();
            _debugLogger.Log($"Auto-starting ReadRecipe on mode change: opId={opId}");
            return state with
            {
                Busy = BusyKind.Transferring,
                ActiveOperationId = opId,
                ActiveFilePath = null
            };
        }

        private bool ShouldAutoRead(AppState s)
        {
            var canStart = s.Busy is BusyKind.Idle or BusyKind.Executing && s.ActiveOperationId is null;
            return s.Mode == AppMode.Runtime && s.EnaSendOk && !s.RecipeActive && canStart;
        }
    }
}