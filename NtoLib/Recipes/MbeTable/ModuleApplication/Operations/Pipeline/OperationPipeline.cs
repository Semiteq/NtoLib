using System;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.Policy;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

public sealed class OperationPipeline
{
    private readonly ILogger<OperationPipeline> _logger;
    private readonly PermissionChecker _permissionChecker;
    private readonly GateFactory _gateFactory;
    private readonly ResultFailureFactory _failureFactory;
    private readonly BlockedResultFactory _blockedFactory;
    private readonly StatusRouter _statusRouter;
    private readonly PostSuccessEffects _postSuccessEffects;
    private readonly UnexpectedExceptionHandler _exceptionHandler;

    public OperationPipeline(
        ILogger<OperationPipeline> logger,
        IStateProvider state,
        PolicyEngine policy,
        IStatusPresenter status)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (state == null) throw new ArgumentNullException(nameof(state));
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (status == null) throw new ArgumentNullException(nameof(status));

        _permissionChecker = new PermissionChecker(state);
        _gateFactory = new GateFactory(state);
        _failureFactory = new ResultFailureFactory();
        _blockedFactory = new BlockedResultFactory();
        _statusRouter = new StatusRouter(policy, status);
        _postSuccessEffects = new PostSuccessEffects(state);
        _exceptionHandler = new UnexpectedExceptionHandler(logger, status);
    }

    public async Task<Result> RunAsync(
        IOperationDefinition op,
        Func<Task<Result>> execute,
        string? successMessage = null)
    {
        if (op == null) throw new ArgumentNullException(nameof(op));
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        var decision = _permissionChecker.Check(op);
        if (decision.Kind != DecisionKind.Allowed)
        {
            LogOperationBlocked(op.Id);
            _statusRouter.ShowBlocked(decision, op);
            return BlockedResultFactory.ToResult(decision);
        }

        var gateResult = _gateFactory.Acquire(op);
        if (gateResult.IsFailed)
        {
            LogOperationBlocked(op.Id);
            _statusRouter.ShowError(gateResult.ToResult(), op);
            return gateResult.ToResult();
        }

        using (gateResult.Value)
        {
            try
            {
                _statusRouter.Clear();

                var operationResult = await execute().ConfigureAwait(false);
                var reasons = ReasonMerger.From(operationResult).ToList();

                if (ResultFailureFactory.ContainsErrors(reasons))
                {
                    LogOperationFailedWithErrors(op.Id);
                    _statusRouter.ShowError(operationResult, op, reasons);
                    return ResultFailureFactory.FromReasons(reasons);
                }

                _statusRouter.PresentCompletion(op, operationResult, reasons, successMessage);

                if (operationResult.IsSuccess)
                {
                    _postSuccessEffects.Apply(op, reasons);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                return _exceptionHandler.Handle(ex, op);
            }
        }
    }

    public async Task<Result<T>> RunAsync<T>(
        IOperationDefinition op,
        Func<Task<Result<T>>> execute,
        string? successMessage = null)
    {
        if (op == null) throw new ArgumentNullException(nameof(op));
        if (execute == null) throw new ArgumentNullException(nameof(execute));

        var decision = _permissionChecker.Check(op);
        if (decision.Kind != DecisionKind.Allowed)
        {
            LogOperationBlocked(op.Id);
            _statusRouter.ShowBlocked(decision, op);
            return BlockedResultFactory.ToResult<T>(decision);
        }

        var gateResult = _gateFactory.Acquire(op);
        if (gateResult.IsFailed)
        {
            LogOperationBlocked(op.Id);
            _statusRouter.ShowError(gateResult.ToResult(), op);
            return gateResult.ToResult<T>();
        }

        using (gateResult.Value)
        {
            try
            {
                _statusRouter.Clear();

                var operationResult = await execute().ConfigureAwait(false);
                var reasons = ReasonMerger.From(operationResult).ToList();

                if (ResultFailureFactory.ContainsErrors(reasons))
                {
                    LogOperationFailedWithErrors(op.Id);
                    _statusRouter.ShowError(operationResult.ToResult(), op, reasons);
                    return ResultFailureFactory.FromReasons<T>(reasons);
                }

                _statusRouter.PresentCompletion(op, operationResult.ToResult(), reasons, successMessage);

                if (operationResult.IsSuccess)
                {
                    _postSuccessEffects.Apply(op, reasons);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                return _exceptionHandler.Handle<T>(ex, op);
            }
        }
    }

    private void LogOperationBlocked(OperationId operationId) =>
        _logger.LogInformation("Operation [{Operation}] blocked before start", operationId);

    private void LogOperationFailedWithErrors(OperationId operationId) =>
        _logger.LogWarning("Operation [{Operation}] finished with error reasons, forcing failure", operationId);
}