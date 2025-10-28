using System;
using System.Threading;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

// Thread-safe implementation of IStateProvider with policy and operation gate.
public sealed class StateProvider : IStateProvider
{
    private readonly object _lock = new();
    private readonly ErrorDefinitionRegistry _registry;
    private readonly ILogger<StateProvider> _logger;
    
    private bool _isValid;
    private int _stepCount;
    private bool _enaSendOk;
    private bool _recipeActive;
    private OperationKind? _activeOperation;

    public event Action<UiPermissions>? PermissionsChanged;

    public StateProvider(ErrorDefinitionRegistry registry, ILogger<StateProvider> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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
                    "Operation evaluation: [{Operation}] -> BLOCKED. Reason: {ReasonCode}. State at decision time: {State}",
                    operation, decision.PrimaryCode, currentState);

                var definition = _registry.GetDefinition(decision.PrimaryCode);
                if (definition.Severity == ErrorSeverity.Warning)
                    return ResultBox.Warn<IDisposable>(default!, decision.PrimaryCode);

                return ResultBox.Fail<IDisposable>(decision.PrimaryCode);
            }

            if (_activeOperation != null)
            {
                _logger.LogWarning(
                    "Operation evaluation: [{Operation}] -> BLOCKED. Reason: Another operation is already active ({ActiveOperation}). State at decision time: {State}",
                    operation, _activeOperation, currentState);
                return ResultBox.Fail<IDisposable>(Codes.CoreInvalidOperation);
            }

            _logger.LogInformation("Operation evaluation: [{Operation}] -> ALLOWED. State at decision time: {State}",
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
            _logger.LogTrace("State changed: Validation. Old: {OldValue}, New: {NewValue}. Current state: {State}",
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
            _logger.LogTrace("State changed: StepCount. Old: {OldValue}, New: {NewValue}. Current state: {State}",
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
                "State changed: PLC Flags. Old (EnaSend/Active): {OldValue}, New (EnaSend/Active): {NewValue}. Current state: {State}",
                oldValue, (enaSendOk, recipeActive), GetSnapshot());
            RaisePermissionsChanged();
        }
    }

    private OperationDecision EvaluateUnsafe(OperationId operation)
    {
        if (_activeOperation != null)
            return OperationDecision.BlockedError(Codes.CoreInvalidOperation);

        if (_recipeActive)
        {
            if (operation is OperationId.Receive or OperationId.Save) return OperationDecision.Allowed();
            return OperationDecision.BlockedError(Codes.CoreInvalidOperation);
        }

        var errorCode = GetBlockingErrorCode(operation);
        if (errorCode != null)
        {
             var definition = _registry.GetDefinition(errorCode.Value);
             return definition.Severity == ErrorSeverity.Warning
                ? OperationDecision.BlockedWarning(errorCode.Value)
                : OperationDecision.BlockedError(errorCode.Value);
        }

        return OperationDecision.Allowed();
    }
    
    private Codes? GetBlockingErrorCode(OperationId operation)
    {
        switch (operation)
        {
            case OperationId.Save:
                if (!_isValid) return Codes.CoreForLoopError;
                if (_stepCount == 0) return Codes.CoreEmptyRecipe;
                break;

            case OperationId.Send:
                if (!_isValid) return Codes.CoreForLoopError;
                if (_stepCount == 0) return Codes.CoreEmptyRecipe;
                break;
                
            // Operations allowed without validation checks
            case OperationId.Load:
            case OperationId.Receive:
            case OperationId.AddStep:
            case OperationId.RemoveStep:
            case OperationId.EditCell:
                break;

            default:
                // Block unknown operations by default
                return Codes.CoreInvalidOperation;
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
            // Swallow exceptions from subscribers
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