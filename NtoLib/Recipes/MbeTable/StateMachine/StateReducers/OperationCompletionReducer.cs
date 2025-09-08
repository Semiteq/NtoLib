#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.StateMachine.App;

namespace NtoLib.Recipes.MbeTable.StateMachine.StateReducers
{
    /// <summary>
    /// Handles the completion of long-running operations.
    /// </summary>
    internal sealed class OperationCompletionReducer : ICommandReducer
    {
        private readonly ILogger _debugLogger;

        public OperationCompletionReducer(ILogger debugLogger)
        {
            _debugLogger = debugLogger;
        }

        public AppState Reduce(AppState currentState, AppCommand command)
        {
            return command switch
            {
                LoadRecipeCompleted(var opId, var s, var m, var e) => HandleCompletion(currentState, opId, s, m, s ? MessageTag.LoadSuccess : MessageTag.LoadError, e),
                SaveRecipeCompleted(var opId, var s, var m, var e) => HandleCompletion(currentState, opId, s, m, s ? MessageTag.SaveSuccess : MessageTag.SaveError, e),
                SendRecipeCompleted(var opId, var s, var m, var e) => HandleCompletion(currentState, opId, s, m, s ? MessageTag.TransferSuccess : MessageTag.TransferError, e),
                ReadRecipeCompleted(var opId, var s, var m, var e) => HandleCompletion(currentState, opId, s, m, s ? MessageTag.TransferSuccess : MessageTag.TransferError, e),
                _ => currentState
            };
        }

        private AppState HandleCompletion(AppState state, Guid opId, bool success, string message, MessageTag tag, IReadOnlyList<string>? errors)
        {
            if (state.ActiveOperationId != opId)
            {
                _debugLogger.Log($"Stale completion ignored: {tag} opId={opId}");
                return state;
            }

            var busyAfter = state.RecipeActive ? BusyKind.Executing : BusyKind.Idle;
            var newErrors = state.ErrorLog;
            if (errors?.Count > 0)
            {
                var appended = newErrors.AddRange(errors);
                newErrors = appended.Count > ErrorLogLimits.MaxErrors
                    ? appended.Skip(appended.Count - ErrorLogLimits.MaxErrors).ToImmutableList()
                    : appended;
            }

            _debugLogger.Log($"{tag}: {message}");
            return state with
            {
                Busy = busyAfter,
                ActiveOperationId = null,
                ActiveFilePath = null,
                ErrorLog = newErrors,
                Message = new UiMessage(tag, message, success ? StatusKind.Info : StatusKind.Error, !success, !success)
            };
        }
    }
}