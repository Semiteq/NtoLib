#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.StateMachine.App;
using NtoLib.Recipes.MbeTable.StateMachine.StateReducers;

namespace NtoLib.Recipes.MbeTable.StateMachine
{
    /// <summary>
    /// Manages the application's state transitions by orchestrating various command reducers.
    /// It ensures thread-safety and decouples state logic from side effects.
    /// </summary>
    public sealed class AppStateMachine
    {
        private readonly object _lock = new();
        private readonly ILogger _debugLogger;
        private RecipeEffectsHandler? _effectsHandler;

        /// <summary>
        /// A dictionary that maps command types to their corresponding reducer strategies.
        /// </summary>
        private readonly Dictionary<Type, ICommandReducer> _reducers;

        /// <summary>
        /// Gets the current state of the application.
        /// </summary>
        public AppState State { get; private set; } = AppState.Initial(AppMode.Editor);

        /// <summary>
        /// Occurs when the application state has changed.
        /// </summary>
        public event Action<AppState>? StateChanged;

        public AppStateMachine(ILogger debugLogger)
        {
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));

            // Instantiate all the strategy classes (reducers).
            // Pass any required dependencies like the logger.
            var lifecycleReducer = new LifecycleReducer(_debugLogger);
            var internalStateReducer = new InternalStateReducer(_debugLogger);
            var operationRequestReducer = new OperationRequestReducer(_debugLogger);
            var operationCompletionReducer = new OperationCompletionReducer(_debugLogger);
            var messageReducer = new MessageReducer();

            // Register reducers for each command type.
            _reducers = new Dictionary<Type, ICommandReducer>
            {
                // Lifecycle commands
                { typeof(EnterEditor), lifecycleReducer },
                { typeof(EnterRuntime), lifecycleReducer },

                // Internal state change commands
                { typeof(VmLoopValidChanged), internalStateReducer },
                { typeof(PlcAvailabilityChanged), internalStateReducer },

                // UI operation request commands
                { typeof(LoadRecipeRequested), operationRequestReducer },
                { typeof(SaveRecipeRequested), operationRequestReducer },
                { typeof(SendRecipeRequested), operationRequestReducer },
                { typeof(ReadRecipeRequested), operationRequestReducer },

                // Operation completion commands (from effects)
                { typeof(LoadRecipeCompleted), operationCompletionReducer },
                { typeof(SaveRecipeCompleted), operationCompletionReducer },
                { typeof(SendRecipeCompleted), operationCompletionReducer },
                { typeof(ReadRecipeCompleted), operationCompletionReducer },

                // Message-related commands
                { typeof(PostMessage), messageReducer },
                { typeof(AckMessage), messageReducer },
                { typeof(ClearMessage), messageReducer }
            };
        }

        /// <summary>
        /// Connects the state machine to its effect handler.
        /// This should be called once during initialization.
        /// </summary>
        public void InitializeEffects(RecipeEffectsHandler handler)
        {
            _effectsHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Dispatches a command to the state machine to trigger a state transition.
        /// </summary>
        public void Dispatch(AppCommand cmd)
        {
            AppState previousState;
            AppState nextState;
            IReadOnlyList<AppEffect> effects;

            lock (_lock)
            {
                previousState = State;

                // The core logic is now delegated to the appropriate strategy.
                nextState = Reduce(previousState, cmd);

                // Derived state calculation and effect computation remain as cross-cutting concerns.
                nextState = RecalculateDerivedState(nextState);
                State = nextState;
                effects = ComputeEffects(previousState, cmd, nextState);
            }

            Emit(nextState);

            if (effects.Count > 0 && _effectsHandler != null)
            {
                foreach (var effect in effects)
                {
                    _debugLogger.Log($"Effect scheduled: {effect.GetType().Name} opId={effect.OpId}", nameof(Dispatch));
                    _effectsHandler.RunEffect(effect);
                }
            }
        }

        /// <summary>
        /// Finds the appropriate reducer for the command and uses it to generate the next state.
        /// </summary>
        private AppState Reduce(AppState state, AppCommand command)
        {
            // Handle transient message clearing centrally before delegating.
            var stateWithoutMessage = command is AckMessage or ClearMessage or PostMessage
                ? state
                : state with { Message = state.Message is { Sticky: false } ? null : state.Message };

            // Find the correct strategy and delegate the work.
            if (_reducers.TryGetValue(command.GetType(), out var reducer))
            {
                return reducer.Reduce(stateWithoutMessage, command);
            }

            _debugLogger.Log($"No reducer found for command: {command.GetType().Name}");
            return stateWithoutMessage;
        }

        /// <summary>
        /// Calculates derived state properties, like UI permissions, based on the new base state.
        /// This logic is a cross-cutting concern and thus remains in the orchestrator.
        /// </summary>
        private AppState RecalculateDerivedState(AppState baseState)
        {
            var isBusyWithOp = baseState.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring;
            var isExecuting = baseState.RecipeActive;

            // Permissions depend on a combination of state flags.
            var canWrite = baseState.Mode == AppMode.Runtime && baseState.VmOk && baseState.EnaSendOk && !isExecuting && baseState.Busy == BusyKind.Idle;
            var canEditRecipe = baseState.Mode == AppMode.Runtime && !isBusyWithOp && !isExecuting;
            var canSaveFile = baseState.Mode == AppMode.Runtime && !isBusyWithOp;

            var permissions = new UiPermissions(
                CanWriteRecipe: canWrite,
                CanOpenFile: canEditRecipe,
                CanAddStep: canEditRecipe,
                CanDeleteStep: canEditRecipe,
                CanSaveFile: canSaveFile
            );

            return baseState with { Permissions = permissions };
        }

        /// <summary>
        /// Determines which side effects (if any) need to be executed based on the state transition.
        /// This is also a cross-cutting concern.
        /// </summary>
        private static IReadOnlyList<AppEffect> ComputeEffects(AppState prev, AppCommand cmd, AppState next)
        {
            // An effect is only triggered if a new operation was successfully started.
            var newOpId = next.ActiveOperationId.HasValue && prev.ActiveOperationId != next.ActiveOperationId
                ? next.ActiveOperationId.Value
                : (Guid?)null;

            if (!newOpId.HasValue)
            {
                return Array.Empty<AppEffect>();
            }

            var opId = newOpId.Value;

            // Create the declarative effect object based on the command and resulting state.
            return cmd switch
            {
                LoadRecipeRequested(var path) when next.Busy == BusyKind.Loading => new[] { new ReadRecipeEffect(opId, path) },
                SaveRecipeRequested(var path) when next.Busy == BusyKind.Saving => new[] { new SaveRecipeEffect(opId, path) },
                SendRecipeRequested when next.Busy == BusyKind.Transferring => new[] { new SendRecipeEffect(opId) },
                ReadRecipeRequested when next.Busy == BusyKind.Transferring => new[] { new ReceiveRecipeEffect(opId) },

                // Effects can also be triggered by internal state changes (auto-read).
                EnterRuntime or PlcAvailabilityChanged when next.Busy == BusyKind.Transferring => new[] { new ReceiveRecipeEffect(opId) },

                _ => Array.Empty<AppEffect>()
            };
        }

        /// <summary>
        /// Emits the state change event on a background thread to avoid blocking the caller.
        /// </summary>
        private void Emit(AppState snapshot)
        {
            ThreadPool.QueueUserWorkItem(_ => StateChanged?.Invoke(snapshot));
        }
    }
}