using System;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ResultsExtension;

public static class ResultBox
{
    public static Result Ok()
    {
        return Result.Ok();
    }

    public static Result<T> Ok<T>(T value)
    {
        return Result.Ok(value);
    }

    public static Result Fail(Codes code)
    {
        return Result.Fail(new Error(string.Empty).WithMetadata(nameof(Codes), code));
    }

    public static Result<T> Fail<T>(Codes code)
    {
        return Result.Fail<T>(new Error(string.Empty).WithMetadata(nameof(Codes), code));
    }

    public static Result Warn(Codes code)
    {
        return Result.Ok().WithReason(new ValidationIssue(code));
    }

    public static Result<T> Warn<T>(T value, Codes code)
    {
        return Result.Ok(value).WithReason(new ValidationIssue(code));
    }

    public static bool TryGetCode(this IReason reason, out Codes code)
    {
        if (reason is ValidationIssue issue)
        {
            code = issue.Code;
            return true;
        }

        if (reason.Metadata?.TryGetValue(nameof(Codes), out var value) == true && value is Codes c)
        {
            code = c;
            return true;
        }

        code = default;
        return false;
    }

    public static bool TryGetCode(this Result result, out Codes code)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        foreach (var reason in result.Reasons)
        {
            if (reason.TryGetCode(out code))
                return true;
        }

        code = default;
        return false;
    }

    public static bool TryGetCode<T>(this Result<T> result, out Codes code)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        return result.ToResult().TryGetCode(out code);
    }

    public static ResultStatus GetStatus(this Result result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        if (result.IsFailed)
            return ResultStatus.Failure;

        if (result.IsSuccess && result.Reasons.Any(r => r is ValidationIssue))
            return ResultStatus.Warning;

        return ResultStatus.Success;
    }

    public static ResultStatus GetStatus<T>(this Result<T> result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        return result.ToResult().GetStatus();
    }
}