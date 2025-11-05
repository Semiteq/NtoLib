using System;
using System.Collections.Generic;
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

    public static Result Fail(Codes code, string? optionalMessage = null, Dictionary<string, object>? optionalMetadata = null)
    {
        var m = optionalMessage ?? string.Empty;
        return optionalMetadata == null
            ? Result.Fail(new Error(m).WithMetadata(nameof(Codes), code))
            : Result.Fail(new Error(m).WithMetadata(optionalMetadata).WithMetadata(nameof(Codes), code));
    }

    public static Result<T> Fail<T>(Codes code, string? optionalMessage = null, Dictionary<string, object>? optionalMetadata = null)
    {
        var m = optionalMessage ?? string.Empty;
        return optionalMetadata == null
            ? Result.Fail<T>(new Error(m).WithMetadata(nameof(Codes), code))
            : Result.Fail<T>(new Error(m).WithMetadata(optionalMetadata).WithMetadata(nameof(Codes), code));
    }

    public static Result Warn(Codes code, Dictionary<string, object>? optionalMetadata = null)
    {
        return optionalMetadata == null
            ? Result.Ok().WithReason(new ValidationIssue(code))
            : Result.Ok().WithReason(new ValidationIssue(code).WithMetadata(optionalMetadata));
    }

    
    public static Result<T> Warn<T>(T value, Codes code, Dictionary<string, object>? optionalMetadata = null)
    {
        return optionalMetadata == null
            ? Result.Ok(value).WithReason(new ValidationIssue(code))
            : Result.Ok(value).WithReason(new ValidationIssue(code).WithMetadata(optionalMetadata));
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