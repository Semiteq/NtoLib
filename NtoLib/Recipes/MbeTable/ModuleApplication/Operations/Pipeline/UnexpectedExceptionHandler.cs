using System;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.Status;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class UnexpectedExceptionHandler
{
	private readonly ILogger _logger;
	private readonly IStatusPresenter _status;

	public UnexpectedExceptionHandler(ILogger logger, IStatusPresenter status)
	{
		_logger = logger;
		_status = status;
	}

	public Result Handle(Exception ex, IOperationDefinition op)
	{
		_logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", op.DisplayNameRu);
		var error = new ApplicationUnexpectedOperationError(ex.Message).CausedBy(ex);
		var msg = StatusPresenter.BuildErrorMessage(error, op.DisplayNameRu);
		_status.ShowError(msg);
		return error;
	}

	public Result<T> Handle<T>(Exception ex, IOperationDefinition op)
	{
		_logger.LogError(ex, "Operation [{Operation}] failed unexpectedly", op.DisplayNameRu);
		var error = new ApplicationUnexpectedOperationError(ex.Message).CausedBy(ex);
		var msg = StatusPresenter.BuildErrorMessage(error, op.DisplayNameRu);
		_status.ShowError(msg);
		return Result.Fail<T>(error);
	}
}
