using System.Collections.Generic;
using System.Linq;

using FluentResults;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class ResultFailureFactory
{
	public static bool ContainsErrors(IEnumerable<IReason> reasons)
	{
		return reasons.OfType<IError>().Any();
	}

	public static Result FromReasons(IEnumerable<IReason> reasons)
	{
		var errors = reasons.OfType<IError>().ToArray();
		return Result.Fail(errors);
	}

	public static Result<T> FromReasons<T>(IEnumerable<IReason> reasons)
	{
		var errors = reasons.OfType<IError>().ToArray();
		return Result.Fail<T>(errors);
	}
}
