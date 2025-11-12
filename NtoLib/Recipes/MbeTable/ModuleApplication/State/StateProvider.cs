using System;
using System.Threading;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;
using NtoLib.Recipes.MbeTable.ModuleApplication.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Warnings;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public sealed class StateProvider : IStateProvider
{
    private readonly object _lock = new();
    private readonly ErrorPolicyRegistry _policyRegistry;
    private readonly ILogger<StateProvider> _logger;
    
    private bool _isValid;
    private int _stepCount;
    private bool _enaSendOk;
    private bool _recipeActive;
    private OperationKind? _activeOperation;

    public event Action<UiPermissions>? PermissionsChanged;
    
    public event Action<bool>? RecipeConsistencyChanged;

    public StateProvider(ErrorPolicyRegistry policyRegistry, ILogger<StateProvider> logger)
    {
        _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                IsValid: _isValid,
                StepCount: _stepCount,
                EnaSendOk: _enaSendOk,
                RecipeActive: _recipeActive,
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

                if (decision.PrimaryReason != null)
                {
                    var severity = _policyRegistry.GetSeverity(decision.PrimaryReason);
                    
                    if (severity == ErrorSeverity.Warning && decision.PrimaryReason is BilingualWarning warning)
                        return Result.Ok().WithReason(warning);

                    if (decision.PrimaryReason is BilingualError error)
                        return error;
                }

                return new ApplicationInvalidOperationError("Operation not allowed");
            }

            if (_activeOperation != null)
            {
                _logger.LogWarning(
                    "Operation evaluation: [{Operation}] -> BLOCKED. Active: {ActiveOperation}. State: {State}",
                    operation, _activeOperation, currentState);
                return new ApplicationAnotherOperationActiveError();
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

    public void SetValidation(bool isValid)
    {
        bool changed = false;
        bool oldValue = _isValid;
        lock (_lock)
        {
            if (_isValid != isValid)
            {
                _isValid = isValid;
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogTrace("State changed: Validation. Old: {OldValue}, New: {NewValue}. State: {State}",
                oldValue, isValid, GetSnapshot());
            RaisePermissionsChanged();
        }
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

        var blockingReason = GetBlockingReason(operation);
        if (blockingReason != null)
        {
            var severity = _policyRegistry.GetSeverity(blockingReason);
            return severity == ErrorSeverity.Warning
                ? OperationDecision.BlockedWarning(blockingReason)
                : OperationDecision.BlockedError(blockingReason);
        }

        return OperationDecision.Allowed();
    }
    
    private IReason? GetBlockingReason(OperationId operation)
    {
        switch (operation)
        {
            case OperationId.Save:
            case OperationId.Send:
                if (!_isValid)
                    return new ApplicationValidationFailedError("Recipe contains validation errors");
                if (_stepCount == 0)
                    return new ApplicationEmptyRecipeWarning();
                break;

            case OperationId.Load:
            case OperationId.Receive:
            case OperationId.AddStep:
            case OperationId.RemoveStep:
            case OperationId.EditCell:
                break;

            default:
                return new ApplicationInvalidOperationError("Unknown operation");
        }

        return null;
    }

    private void RaisePermissionsChanged()
    {
        try
        {
            PermissionsChanged?.Invoke(GetUiPermissions());
        }
        catch
        {
        }
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