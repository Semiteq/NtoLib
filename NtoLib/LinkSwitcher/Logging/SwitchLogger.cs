using System.Collections.Generic;

using NtoLib.LinkSwitcher.Entities;

using Serilog;

namespace NtoLib.LinkSwitcher.Logging;

public sealed class SwitchLogger
{
	private readonly ILogger _logger;

	public SwitchLogger(ILogger logger)
	{
		_logger = logger;
	}

	public void LogScanHeader(string searchPath, bool forward)
	{
		var direction = forward ? "Forward (Name -> Name2)" : "Reverse (Name2 -> Name)";
		_logger.Information("=== LinkSwitcherFB: Scan === Path: {SearchPath}, Direction: {Direction}", searchPath, direction);
	}

	public void LogDiscoveredPairs(IReadOnlyList<ObjectPair> pairs)
	{
		if (pairs.Count == 0)
		{
			_logger.Warning("No pairs discovered");
			return;
		}

		_logger.Information("Discovered {PairCount} pairs", pairs.Count);
		for (var i = 0; i < pairs.Count; i++)
		{
			_logger.Information("  [{Index}] {SourceName} / {TargetName}", i + 1, pairs[i].Source.Name, pairs[i].Target.Name);
		}
	}

	public void LogValidationSuccess(ObjectPair pair, int linkCount)
	{
		_logger.Information("  {PairName}: structure OK, links to transfer: {LinkCount}", pair.Name, linkCount);
	}

	public void LogValidationFailure(ObjectPair pair, string reason)
	{
		_logger.Error("  {PairName}: VALIDATION FAILED -- {Reason}", pair.Name, reason);
	}

	public void LogPlan(IReadOnlyList<LinkOperation> operations)
	{
		if (operations.Count == 0)
		{
			_logger.Information("No links to transfer");
			return;
		}

		_logger.Information("Links to transfer ({OperationCount}):", operations.Count);
		foreach (var operation in operations)
		{
			var direction = FormatDirection(operation);
			_logger.Information("  {SourcePin} {Direction} {ExternalPin} => {TargetPin}",
				operation.SourcePinPath, direction, operation.ExternalPinPath, operation.TargetPinPath);
		}
	}

	public void LogDryRunComplete()
	{
		_logger.Information("=== DryRun complete: no links were modified ===");
	}

	public void LogExecutionHeader()
	{
		_logger.Information("=== LinkSwitcherFB: Execution ===");
	}

	public void LogOperationSuccess(LinkOperation operation)
	{
		var direction = FormatDirection(operation);
		_logger.Information("  [OK] {SourcePin} {Direction} {ExternalPin} => {TargetPin}",
			operation.SourcePinPath, direction, operation.ExternalPinPath, operation.TargetPinPath);
	}

	public void LogOperationFailure(LinkOperation operation, string error)
	{
		var direction = FormatDirection(operation);
		_logger.Error("  [ERROR] {SourcePin} {Direction} {ExternalPin}: {Error}",
			operation.SourcePinPath, direction, operation.ExternalPinPath, error);
	}

	public void LogExecutionSummary(int totalCount, int successCount, int failureCount)
	{
		if (failureCount == 0)
		{
			_logger.Information("Result: all {TotalCount} links transferred successfully", totalCount);
		}
		else
		{
			_logger.Warning("Result: {SuccessCount} of {TotalCount} links transferred, {FailureCount} errors",
				successCount, totalCount, failureCount);
		}
	}

	public void LogCancellation()
	{
		_logger.Information("=== LinkSwitcherFB: Operation cancelled by user ===");
	}

	public void LogError(string message)
	{
		_logger.Error("ERROR: {ErrorMessage}", message);
	}

	private static string FormatDirection(LinkOperation operation)
	{
		if (operation.IsIConnect)
			return "<=>";
		return operation.IsIncoming ? "<-" : "->";
	}
}
