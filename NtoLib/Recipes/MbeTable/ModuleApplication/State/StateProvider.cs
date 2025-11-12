using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed class StateProvider : IStateProvider
{
    private readonly object _lock = new();
    private readonly ErrorPolicyRegistry _policyRegistry;
    private readonly ILogger<StateProvider> _logger;

    private int _stepCount;
    private bool _enaSendOk;
    private bool _recipeActive;
    private bool _isRecipeConsistent;
    private OperationKind? _activeOperation;

    private IReadOnlyList<IReason> _policyReasons = Array.Empty<IReason>();

    public event Action<UiPermissions>? PermissionsChanged;
    public event Action<bool>? RecipeConsistencyChanged;

    public StateProvider(ErrorPolicyRegistry policyRegistry, ILogger<StateProvider> logger)
    {
        _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Default: recipe is not consistent until explicitly set after successful send.
        _isRecipeConsistent = false;
    }

    public UiPermissions GetUiPermissions()
    {
        lock (_lock)
        {
            return new UiPermissions(
                CanWriteRecipe: EvaluateUnsafe(OperationId.Send).Kind == DecisionKind.Allowed,
                CanOpenFile: EvaluateUnsafe(OperationId.Load).Kind == DecisionKind.Allowed,
                CanAddStep: EvaluateUnsafe(OperationId.AddStep).Kind == DecisionKind.Allowed,
                CanDeleteStep: EvaluateUnsafe(OperationId.RemoveStep).Kind == DecisionKind.Allowed,
                CanSaveFile: EvaluateUnsafe(OperationId.Save).Kind == DecisionKind.Allowed,
                IsGridReadOnly: _recipeActive
            );
        }
    }

    public UiStateSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new UiStateSnapshot(
                StepCount: _stepCount,
                EnaSendOk: _enaSendOk,
                RecipeActive: _recipeActive,
                IsRecipeConsistent: _isRecipeConsistent,
                ActiveOperation: _activeOperation
            );
        }
    }

    public OperationDecision Evaluate(OperationId operation)
    {
        lock (_lock)
        {
            return EvaluateUnsafe(operation);
        }
    }

    public Result<IDisposable> BeginOperation(OperationKind kind, OperationId operation)
    {
        lock (_lock)
        {
            var decision = EvaluateUnsafe(operation);
            var currentState = GetSnapshot();

            if (decision.Kind != DecisionKind.Allowed)
            {
                _logger.LogInformation(
                    "Operation evaluation: [{Operation}] -> BLOCKED. Reason: {Reason}. State: {State}",
                    operation, decision.PrimaryReason?.GetType().Name, currentState);

                if (decision.PrimaryReason is BilingualError error)
                    return Result.Fail<IDisposable>(error);

                if (decision.PrimaryReason is BilingualWarning warning)
                {
                    var fail = Result.Fail<IDisposable>(new ApplicationInvalidOperationError("Operation not allowed"));
                    return fail.WithReason(warning);
                }

                return Result.Fail<IDisposable>(new ApplicationInvalidOperationError("Operation not allowed"));
            }

            if (_activeOperation != null)
            {
                _logger.LogWarning(
                    "Operation evaluation: [{Operation}] -> BLOCKED. Active: {ActiveOperation}. State: {State}",
                    operation, _activeOperation, currentState);
                return Result.Fail<IDisposable>(new ApplicationAnotherOperationActiveError());
            }

            _logger.LogInformation("Operation evaluation: [{Operation}] -> ALLOWED. State: {State}",
                operation, currentState);

            _activeOperation = kind;
        }

        RaisePermissionsChanged();
        return Result.Ok<IDisposable>(new Gate(this));
    }

    public void EndOperation()
    {
        bool changed = false;
        lock (_lock)
        {
            if (_activeOperation != null)
            {
                _activeOperation = null;
                changed = true;
            }
        }
        if (changed) RaisePermissionsChanged();
    }

    public void SetStepCount(int stepCount)
    {
        bool changed = false;
        int oldValue = _stepCount;
        lock (_lock)
        {
            if (_stepCount != stepCount)
            {
                _stepCount = stepCount;
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogTrace("State changed: StepCount. Old: {OldValue}, New: {NewValue}. State: {State}",
                oldValue, stepCount, GetSnapshot());
            RaisePermissionsChanged();
        }
    }

    public void SetPlcFlags(bool enaSendOk, bool recipeActive)
    {
        bool changed = false;
        var oldValue = (_enaSendOk, _recipeActive);
        lock (_lock)
        {
            if (_enaSendOk != enaSendOk || _recipeActive != recipeActive)
            {
                _enaSendOk = enaSendOk;
                _recipeActive = recipeActive;
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogTrace(
                "State changed: PLC Flags. Old: {OldValue}, New: {NewValue}. State: {State}",
                oldValue, (enaSendOk, recipeActive), GetSnapshot());
            RaisePermissionsChanged();
        }
    }

    public void SetPolicyReasons(IEnumerable<IReason> reasons)
    {
        var newList = (reasons ?? Enumerable.Empty<IReason>()).ToList().AsReadOnly();
        bool changed = false;
        lock (_lock)
        {
            if (!ReferenceEquals(_policyReasons, newList))
            {
                _policyReasons = newList;
                changed = true;
            }
        }
        if (changed) RaisePermissionsChanged();
    }

    public void SetRecipeConsistent(bool isConsistent)
    {
        bool changed = false;
        bool oldValue;
        lock (_lock)
        {
            oldValue = _isRecipeConsistent;
            if (_isRecipeConsistent != isConsistent)
            {
                _isRecipeConsistent = isConsistent;
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogTrace("State changed: RecipeConsistent. Old: {OldValue}, New: {NewValue}.", oldValue, isConsistent);
            try { RecipeConsistencyChanged?.Invoke(isConsistent); } catch { }
        }
    }

    private OperationDecision EvaluateUnsafe(OperationId operation)
    {
        if (_activeOperation != null)
        {
            return OperationDecision.BlockedError(new ApplicationAnotherOperationActiveError());
        }

        if (_recipeActive)
        {
            if (operation is OperationId.Receive or OperationId.Save)
                return OperationDecision.Allowed();

            return OperationDecision.BlockedError(new ApplicationRecipeActiveError());
        }

        var scope = MapOperationToScope(operation);
        foreach (var r in _policyReasons)
        {
            if (_policyRegistry.Blocks(r, scope))
            {
                var severity = _policyRegistry.GetSeverity(r);
                return severity == ErrorSeverity.Warning
                    ? OperationDecision.BlockedWarning(r)
                    : OperationDecision.BlockedError(r);
            }
        }

        return OperationDecision.Allowed();
    }

    private static BlockingScope MapOperationToScope(OperationId operation) =>
        operation switch
        {
            OperationId.Save => BlockingScope.Save,
            OperationId.Send => BlockingScope.Send,
            OperationId.Load => BlockingScope.Load,
            OperationId.Receive => BlockingScope.Load,
            OperationId.AddStep => BlockingScope.Edit,
            OperationId.RemoveStep => BlockingScope.Edit,
            OperationId.EditCell => BlockingScope.Edit,
            _ => BlockingScope.None
        };

    private void RaisePermissionsChanged()
    {
        try { PermissionsChanged?.Invoke(GetUiPermissions()); } catch { }
    }

    private sealed class Gate : IDisposable
    {
        private readonly StateProvider _owner;
        private int _disposed;

        public Gate(StateProvider owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _owner.EndOperation();
        }
    }
}