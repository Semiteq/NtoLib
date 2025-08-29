#nullable enable

using System;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.StateMachine.App;

namespace NtoLib.Recipes.MbeTable.StateMachine.StateReducers
{
    /// <summary>
    /// Handles commands related to internal system state changes, such as PLC availability or VM validation status.
    /// </summary>
    internal sealed class InternalStateReducer : ICommandReducer
    {
        private readonly ILogger _debugLogger;

        public InternalStateReducer(ILogger debugLogger)
        {
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        }

        public AppState Reduce(AppState currentState, AppCommand command)
        {
            return command switch
            {
                VmLoopValidChanged(var vmOk) => HandleVmLoopValidChanged(currentState, vmOk),
                PlcAvailabilityChanged(var avail) => HandlePlcAvailabilityChanged(currentState, avail),
                _ => currentState
            };
        }

        /// <summary>
        /// Updates the state based on changes in the PLC's availability and recipe status.
        /// </summary>
        private AppState HandlePlcAvailabilityChanged(AppState state, PlcRecipeAvailable avail)
        {
            var isExecuting = avail.IsRecipeActive;

            // Do not override busy status if an operation like Loading/Saving is in progress.
            var newBusy = state.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring
                ? state.Busy
                : (isExecuting ? BusyKind.Executing : BusyKind.Idle);

            var nextState = state with
            {
                EnaSendOk = avail.IsEnaSend,
                RecipeActive = isExecuting,
                Busy = newBusy
            };

            _debugLogger.Log($"PLC availability changed: EnaSend={avail.IsEnaSend}, RecipeActive={avail.IsRecipeActive}", nameof(HandlePlcAvailabilityChanged));

            // Encapsulated logic to trigger an auto-read operation if conditions are met.
            return MaybeStartAutoRead(nextState);
        }

        /// <summary>
        /// Updates the state based on the validity of the recipe view model.
        /// </summary>
        private AppState HandleVmLoopValidChanged(AppState state, bool vmOk)
        {
            var newState = state with { VmOk = vmOk };

            if (vmOk && newState.Message?.Tag == MessageTag.VmInvalid)
            {
                // If the VM becomes valid, clear the specific sticky error message.
                _debugLogger.Log("VM loop is now valid, clearing VmInvalid error message.");
                return newState with { Message = null };
            }

            if (!vmOk && newState.Message?.Tag != MessageTag.VmInvalid)
            {
                // If the VM is invalid, post a specific sticky error message.
                _debugLogger.Log("VM loop is invalid, posting VmInvalid error message.");
                return newState with
                {
                    Message = new UiMessage(MessageTag.VmInvalid, "Цикл невалиден. Исправьте ошибки.", StatusKind.Error, Sticky: true, AckRequired: false)
                };
            }

            return newState;
        }

        /// <summary>
        /// Checks if an automatic recipe read from the PLC should be initiated and modifies the state accordingly.
        /// </summary>
        private AppState MaybeStartAutoRead(AppState state)
        {
            if (!ShouldAutoRead(state))
            {
                return state;
            }

            var opId = Guid.NewGuid();
            _debugLogger.Log($"Auto-starting ReadRecipe on PLC availability change: opId={opId}");

            // Initiate the 'Transferring' state for the auto-read operation.
            return state with
            {
                Busy = BusyKind.Transferring,
                ActiveOperationId = opId,
                ActiveFilePath = null
            };
        }

        /// <summary>
        /// Determines if the conditions for an automatic recipe read are met.
        /// </summary>
        private bool ShouldAutoRead(AppState s)
        {
            // Auto-read should only happen if no other operation is running.
            var canStartOperation = s.Busy is BusyKind.Idle or BusyKind.Executing && s.ActiveOperationId is null;

            var should = s.Mode == AppMode.Runtime
                         && s.EnaSendOk
                         && !s.RecipeActive
                         && canStartOperation;

            if (should)
            {
                _debugLogger.Log($"ShouldAutoRead check PASSED: mode={s.Mode}, ena={s.EnaSendOk}, active={s.RecipeActive}, canStart={canStartOperation}", nameof(ShouldAutoRead));
            }

            return should;
        }
    }
}