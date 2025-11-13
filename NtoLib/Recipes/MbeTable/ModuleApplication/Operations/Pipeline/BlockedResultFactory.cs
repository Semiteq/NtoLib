using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class BlockedResultFactory
{
    public static Result ToResult(OperationDecision decision)
    {
        if (decision.Kind == DecisionKind.BlockedError)
        {
            if (decision.PrimaryReason is IError err)
                return Result.Fail(err);

            var res = Result.Fail(new ApplicationInvalidOperationError("Operation not allowed"));
            return decision.PrimaryReason != null ? res.WithReason(decision.PrimaryReason) : res;
        }

        var ok = Result.Ok();
        return decision.PrimaryReason != null ? ok.WithReason(decision.PrimaryReason) : ok;
    }

    public static Result<T> ToResult<T>(OperationDecision decision)
    {
        if (decision.Kind == DecisionKind.BlockedError)
        {
            if (decision.PrimaryReason is IError err) return Result.Fail<T>(err);

            var res = Result.Fail<T>(new ApplicationInvalidOperationError("Operation not allowed"));
            return decision.PrimaryReason != null ? res.WithReason(decision.PrimaryReason) : res;
        }

        var ok = Result.Ok<T>(default!);
        return decision.PrimaryReason != null ? ok.WithReason(decision.PrimaryReason) : ok;
    }
}