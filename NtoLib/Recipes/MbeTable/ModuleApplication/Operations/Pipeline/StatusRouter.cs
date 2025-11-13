using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Policy;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;
using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class StatusRouter
{
    private readonly PolicyEngine _policy;
    private readonly IStatusPresenter _status;

    public StatusRouter(PolicyEngine policy, IStatusPresenter status)
    {
        _policy = policy;
        _status = status;
    }

    public void Clear() => _status.Clear();

    public void PresentCompletion(
        IOperationDefinition op,
        Result baseResult,
        IReadOnlyList<IReason> reasons,
        string? successMessage)
    {
        var decision = _policy.Decide(op.Id, reasons);

        if (decision.Kind == DecisionKind.BlockedError)
        {
            ShowError(baseResult, op);
            return;
        }

        if (decision.Kind == DecisionKind.BlockedWarning)
        {
            ShowWarning(baseResult, op);
            return;
        }

        var hasWarnings = reasons.OfType<BilingualWarning>().Any();
        if (hasWarnings)
        {
            ShowWarning(baseResult, op);
            return;
        }

        ShowSuccessIfNeeded(successMessage, op);
    }

    public void ShowBlocked(OperationDecision decision, IOperationDefinition op)
    {
        if (decision.Kind == DecisionKind.BlockedWarning)
        {
            var r = Result.Ok();
            if (decision.PrimaryReason != null)
                r = r.WithReason(decision.PrimaryReason);
            ShowWarning(r, op);
            return;
        }

        if (decision.Kind == DecisionKind.BlockedError)
        {
            Result r;
            if (decision.PrimaryReason is IError err)
            {
                r = Result.Fail(err);
            }
            else
            {
                r = Result.Fail(
                    new NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors.ApplicationInvalidOperationError(
                        "Operation not allowed"));
                if (decision.PrimaryReason != null)
                    r = r.WithReason(decision.PrimaryReason);
            }

            ShowError(r, op);
            return;
        }

        _status.Clear();
    }

    public void ShowError(Result result, IOperationDefinition op)
    {
        var msg = StatusPresenter.BuildErrorMessage(result, op.DisplayNameRu);
        _status.ShowError(msg);
    }

    public void ShowWarning(Result result, IOperationDefinition op)
    {
        var msg = StatusPresenter.BuildWarningMessage(result, op.DisplayNameRu);
        _status.ShowWarning(msg);
    }

    private void ShowSuccessIfNeeded(string? successMessage, IOperationDefinition op)
    {
        if (op.CompletionMessage == CompletionMessageKind.Success && !string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowSuccess(successMessage!);
            return;
        }

        if (op.CompletionMessage == CompletionMessageKind.Info && !string.IsNullOrWhiteSpace(successMessage))
        {
            _status.ShowInfo(successMessage!);
            return;
        }

        _status.Clear();
    }
}