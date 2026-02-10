using System;
using System.Collections.Generic;

using FluentResults;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.LinkSwitcher.Entities;
using NtoLib.LinkSwitcher.Logging;

namespace NtoLib.LinkSwitcher.TreeOperations;

public sealed class LinkExecutor
{
	private readonly IProjectHlp _project;
	private readonly SwitchLogger _logger;

	public LinkExecutor(IProjectHlp project, SwitchLogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result Execute(IReadOnlyList<LinkOperation> operations)
	{
		_logger.LogExecutionHeader();

		var successCount = 0;
		var failureCount = 0;

		foreach (var operation in operations)
		{
			var result = ExecuteOperation(operation);

			if (result.IsSuccess)
			{
				successCount++;
				_logger.LogOperationSuccess(operation);
			}
			else
			{
				failureCount++;
				var errorMessage = string.Join("; ", result.Errors);
				_logger.LogOperationFailure(operation, errorMessage);
			}
		}

		_logger.LogExecutionSummary(operations.Count, successCount, failureCount);

		if (failureCount > 0)
		{
			return Result.Fail($"{failureCount} of {operations.Count} link operations failed.");
		}

		return Result.Ok();
	}

	private Result ExecuteOperation(LinkOperation operation)
	{
		var externalPinHlp = _project.SafeItem<ITreePinHlp>(operation.ExternalPinPath);
		if (externalPinHlp == null)
		{
			return Result.Fail($"External pin not found: {operation.ExternalPinPath}");
		}

		var sourcePinHlp = _project.SafeItem<ITreePinHlp>(operation.SourcePinPath);
		if (sourcePinHlp == null)
		{
			return Result.Fail($"Source pin not found: {operation.SourcePinPath}");
		}

		var targetPinHlp = _project.SafeItem<ITreePinHlp>(operation.TargetPinPath);
		if (targetPinHlp == null)
		{
			return Result.Fail($"Target pin not found: {operation.TargetPinPath}");
		}

		try
		{
			if (operation.IsIConnect)
			{
				sourcePinHlp.Disconnect(externalPinHlp);
				targetPinHlp.Connect(externalPinHlp, EConnectionType.ctIConnect);
			}
			else if (operation.IsIncoming)
			{
				sourcePinHlp.Disconnect(externalPinHlp);
				targetPinHlp.Connect(externalPinHlp);
			}
			else
			{
				externalPinHlp.Disconnect(sourcePinHlp);
				externalPinHlp.Connect(targetPinHlp);
			}
		}
		catch (Exception exception)
		{
			return Result.Fail($"Link operation failed: {exception.Message}");
		}

		return Result.Ok();
	}
}
