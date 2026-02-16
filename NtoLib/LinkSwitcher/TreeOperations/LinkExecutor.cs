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

	public Result Execute(IReadOnlyList<PairOperations> pairResults, string sourcePath, string targetPath)
	{
		_logger.LogExecutionHeader();

		var totalSuccess = 0;
		var totalFailure = 0;
		var totalCount = 0;

		foreach (var pairOps in pairResults)
		{
			if (pairOps.Operations.Count == 0)
				continue;

			_logger.LogPairExecutionHeader(pairOps.Pair);
			var columnWidths = SwitchLogger.ComputeColumnWidths(pairOps.Operations, sourcePath, targetPath);

			foreach (var operation in pairOps.Operations)
			{
				totalCount++;
				var result = ExecuteOperation(operation);

				if (result.IsSuccess)
				{
					totalSuccess++;
					_logger.LogOperationSuccess(operation, sourcePath, targetPath, columnWidths);
				}
				else
				{
					totalFailure++;
					var errorMessage = string.Join("; ", result.Errors);
					_logger.LogOperationFailure(operation, sourcePath, targetPath, columnWidths, errorMessage);
				}
			}
		}

		_logger.LogExecutionSummary(totalCount, totalSuccess, totalFailure);

		if (totalFailure > 0)
		{
			return Result.Fail($"{totalFailure} of {totalCount} link operations failed.");
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
