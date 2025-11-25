using System;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Extension methods for FluentResults to support functional composition.
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Maps a successful Result to a new value.
	/// </summary>
	public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
	{
		if (result.IsFailed)
			return Result.Fail<TOut>(result.Errors);

		return Result.Ok(mapper(result.Value));
	}
}
