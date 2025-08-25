#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine
{
    public sealed class AppStateMachine : IDisposable
    {
        private readonly object _lock = new();
        private RecipeEffectsHandler? _effectsHandler;
        private readonly DebugLogger _debugLogger;

        public AppState State { get; private set; } = AppState.Initial(AppMode.Editor);
        public event Action<AppState>? StateChanged;

        public AppStateMachine(DebugLogger debugLogger)
        {
            _debugLogger = debugLogger;
        }

        public void InitializeEffects(RecipeEffectsHandler handler)
        {
            _effectsHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Dispatch(AppCommand cmd)
        {
            AppState prev, next;
            List<AppEffect> effects;

            lock (_lock)
            {
                prev = State;
                next = Reduce(prev, cmd);
                next = RecalculateDerivedState(next);
                State = next;

                effects = ComputeEffects(prev, cmd, next);
            }

            Emit(next);

            if (effects.Count > 0 && _effectsHandler != null)
            {
                foreach (var eff in effects)
                {
                    _debugLogger.Log($"Effect scheduled: {eff.GetType().Name} opId={eff.OpId}", nameof(Dispatch));
                    _effectsHandler.RunEffect(eff);
                }
            }
        }

        private AppState Reduce(AppState currentState, AppCommand command)
        {
            var state = (command is AckMessage or ClearMessage or PostMessage)
                ? currentState
                : currentState with { Message = currentState.Message is { Sticky: false } ? null : currentState.Message };

            switch (command)
            {
                case EnterEditor:
                {
                    var busy = state.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring
                        ? state.Busy
                        : BusyKind.Idle;
                    return state with
                    {
                        Mode = AppMode.Editor,
                        Busy = busy,
                        ActiveOperationId = state.ActiveOperationId,
                        ActiveFilePath = state.ActiveFilePath
                    };
                }

                case EnterRuntime:
                {
                    var busy = state.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring
                        ? state.Busy
                        : (state.RecipeActive ? BusyKind.Executing : BusyKind.Idle);

                    var next = state with
                    {
                        Mode = AppMode.Runtime,
                        Busy = busy,
                        ActiveOperationId = state.ActiveOperationId,
                        ActiveFilePath = state.ActiveFilePath
                    };

                    // Авто‑чтение при входе в Runtime, если контроллер уже доступен
                    if (ShouldAutoRead(next))
                    {
                        var opId = Guid.NewGuid();
                        _debugLogger.Log($"Auto-start ReadRecipe on EnterRuntime: opId={opId}", nameof(EnterRuntime));
                        next = next with
                        {
                            Busy = BusyKind.Transferring,
                            ActiveOperationId = opId,
                            ActiveFilePath = null
                        };
                    }

                    return next;
                }

                case VmLoopValidChanged(var vmOk):
                    return OnVmLoopValidChanged(state, vmOk);

                case PlcAvailabilityChanged(var avail):
                {
                    var isExecuting = avail.IsRecipeActive;
                    var newBusy = state.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring
                        ? state.Busy
                        : (isExecuting ? BusyKind.Executing : BusyKind.Idle);

                    var tmp = state with
                    {
                        EnaSendOk = avail.IsEnaSend,
                        RecipeActive = isExecuting,
                        Busy = newBusy
                    };

                    _debugLogger.Log($"[Reduce] PLC availability changed: {avail.IsRecipeActive}, {avail.IsEnaSend}");

                    // Авто‑чтение при смене доступности (если уже в Runtime и условия выполняются)
                    if (ShouldAutoRead(tmp))
                    {
                        var opId = Guid.NewGuid();
                        _debugLogger.Log($"Auto-start ReadRecipe on PlcAvailabilityChanged: opId={opId}");
                        tmp = tmp with
                        {
                            Busy = BusyKind.Transferring,
                            ActiveOperationId = opId,
                            ActiveFilePath = null
                        };
                    }

                    return tmp;
                }

                case LoadRecipeRequested(var filePath):
                    if (!CanStartOperation(state))
                        return DenyNewOpWithMessage(state, "Операция уже выполняется.");
                    return state with
                    {
                        Busy = BusyKind.Loading,
                        ActiveOperationId = Guid.NewGuid(),
                        ActiveFilePath = filePath
                    };

                case SaveRecipeRequested(var filePath):
                    if (!CanStartOperation(state))
                        return DenyNewOpWithMessage(state, "Операция уже выполняется.");
                    return state with
                    {
                        Busy = BusyKind.Saving,
                        ActiveOperationId = Guid.NewGuid(),
                        ActiveFilePath = filePath
                    };

                case SendRecipeRequested:
                    if (!CanStartOperation(state))
                        return DenyNewOpWithMessage(state, "Операция уже выполняется.");
                    return state with
                    {
                        Busy = BusyKind.Transferring,
                        ActiveOperationId = Guid.NewGuid(),
                        ActiveFilePath = null
                    };

                case ReadRecipeRequested:
                    if (!CanStartOperation(state))
                        return DenyNewOpWithMessage(state, "Операция уже выполняется.");
                    return state with
                    {
                        Busy = BusyKind.Transferring,
                        ActiveOperationId = Guid.NewGuid(),
                        ActiveFilePath = null
                    };

                case LoadRecipeCompleted(var opId, var success, var msg, var errors):
                    return OnOperationCompleted(state, opId, success, msg, success ? MessageTag.LoadSuccess : MessageTag.LoadError, errors);

                case SaveRecipeCompleted(var opIdS, var successS, var msgS, var errorsS):
                    return OnOperationCompleted(state, opIdS, successS, msgS, successS ? MessageTag.SaveSuccess : MessageTag.SaveError, errorsS);

                case SendRecipeCompleted(var opIdT, var successT, var msgT, var errorsT):
                    return OnOperationCompleted(state, opIdT, successT, msgT, successT ? MessageTag.TransferSuccess : MessageTag.TransferError, errorsT);

                case ReadRecipeCompleted(var opIdR, var successR, var msgR, var errorsR):
                    return OnOperationCompleted(state, opIdR, successR, msgR, successR ? MessageTag.TransferSuccess : MessageTag.TransferError, errorsR);

                case PostMessage(var msg):
                    return state with { Message = msg };

                case AckMessage when state.Message?.AckRequired == true:
                    return state with { Message = null };

                case ClearMessage:
                    return state with { Message = null };

                default:
                    return state;
            }
        }

        private static bool CanStartOperation(AppState s)
        {
            return s.Busy is BusyKind.Idle or BusyKind.Executing && s.ActiveOperationId is null;
        }

        private bool ShouldAutoRead(AppState s)
        {
            var noActiveOp = s.ActiveOperationId is null;
            var notBusyOp = s.Busy is BusyKind.Idle or BusyKind.Executing;
            var should = s.Mode == AppMode.Runtime
                         && s.EnaSendOk
                         && !s.RecipeActive
                         && noActiveOp
                         && notBusyOp;

            _debugLogger.Log($"ShouldAutoRead: mode={s.Mode}, ena={s.EnaSendOk}, exec={s.RecipeActive}, busy={s.Busy}, opId={(s.ActiveOperationId?.ToString() ?? "null")} => {should}", nameof(ShouldAutoRead));
            return should;
        }

        private AppState DenyNewOpWithMessage(AppState s, string text)
        {
            _debugLogger.Log("Operation denied: already running");
            return s with
            {
                Message = new UiMessage(MessageTag.None, text, StatusKind.Warning, Sticky: false, AckRequired: false)
            };
        }

        private AppState OnVmLoopValidChanged(AppState state, bool vmOk)
        {
            var newState = state with { VmOk = vmOk };
            if (vmOk && newState.Message?.Tag == MessageTag.VmInvalid)
            {
                _debugLogger.Log("VM loop is valid, clearing error message");
                return newState with { Message = null };
            }
            if (!vmOk && newState.Message?.Tag != MessageTag.VmInvalid)
            {
                _debugLogger.Log("VM loop is invalid, showing error message");
                return newState with
                {
                    Message = new UiMessage(MessageTag.VmInvalid, "Цикл невалиден. Исправьте ошибки.", StatusKind.Error, Sticky: true, AckRequired: false)
                };
            }
            return newState;
        }

        private AppState OnOperationCompleted(AppState state, Guid opId, bool success, string message, MessageTag tag, IReadOnlyList<string>? errors)
        {
            if (state.ActiveOperationId is null || state.ActiveOperationId.Value != opId)
            {
                _debugLogger.Log($"Stale completion ignored: {tag} opId={opId}");
                return state;
            }

            var backBusy = state.RecipeActive ? BusyKind.Executing : BusyKind.Idle;
            var kind = success ? StatusKind.Info : StatusKind.Error;

            var newErrors = state.ErrorLog;
            if (!success && errors != null && errors.Count > 0)
            {
                var appended = newErrors.AddRange(errors);
                if (appended.Count > ErrorLogLimits.MaxErrors)
                    appended = appended.Skip(appended.Count - ErrorLogLimits.MaxErrors).ToImmutableList();
                newErrors = appended;
            }

            _debugLogger.Log($"{tag}: {message}");
            return state with
            {
                Busy = backBusy,
                ActiveOperationId = null,
                ActiveFilePath = null,
                ErrorLog = newErrors,
                Message = new UiMessage(tag, message, kind, Sticky: !success, AckRequired: !success)
            };
        }

        private AppState RecalculateDerivedState(AppState baseState)
        {
            var isBusyWithOp = baseState.Busy is BusyKind.Loading or BusyKind.Saving or BusyKind.Transferring;
            var isExecuting = baseState.RecipeActive;

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

            _debugLogger.Log($"Permissions: {permissions}", nameof(RecalculateDerivedState));
            return baseState with { Permissions = permissions };
        }

        private List<AppEffect> ComputeEffects(AppState prev, AppCommand cmd, AppState next)
        {
            var list = new List<AppEffect>();

            if (_effectsHandler == null)
                return list;

            if (cmd is LoadRecipeRequested && next.Busy == BusyKind.Loading && next.ActiveOperationId is Guid op1 && !string.IsNullOrEmpty(next.ActiveFilePath))
            {
                list.Add(new LoadRecipeEffect(op1, next.ActiveFilePath!));
            }
            else if (cmd is SaveRecipeRequested && next.Busy == BusyKind.Saving && next.ActiveOperationId is Guid op2 && !string.IsNullOrEmpty(next.ActiveFilePath))
            {
                list.Add(new SaveRecipeEffect(op2, next.ActiveFilePath!));
            }
            else if (cmd is SendRecipeRequested && next.Busy == BusyKind.Transferring && next.ActiveOperationId is Guid op3)
            {
                list.Add(new SendRecipeEffect(op3));
            }
            else if (cmd is ReadRecipeRequested && next.Busy == BusyKind.Transferring && next.ActiveOperationId is Guid op4)
            {
                list.Add(new ReadRecipeEffect(op4));
            }
            else if (cmd is PlcAvailabilityChanged && next.Busy == BusyKind.Transferring && next.ActiveOperationId is Guid opAuto1 && prev.ActiveOperationId != next.ActiveOperationId)
            {
                list.Add(new ReadRecipeEffect(opAuto1));
            }
            else if (cmd is EnterRuntime && next.Busy == BusyKind.Transferring && next.ActiveOperationId is Guid opAuto2 && prev.ActiveOperationId != next.ActiveOperationId)
            {
                list.Add(new ReadRecipeEffect(opAuto2));
            }

            return list;
        }

        private void Emit(AppState snapshot)
        {
            ThreadPool.QueueUserWorkItem(_ => StateChanged?.Invoke(snapshot));
        }

        public void Dispose() { }
    }
}