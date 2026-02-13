using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.LinkSwitcher.Entities;

using Serilog;

namespace NtoLib.LinkSwitcher.Logging;

public sealed class SwitchLogger
{
	private const string DirectTag = "[direct]  ";
	private const string FeedbackTag = "[feedback]";

	private readonly ILogger _logger;

	public SwitchLogger(ILogger logger)
	{
		_logger = logger;
	}

	public void LogScanHeader(string searchPath, bool reverse)
	{
		var direction = reverse ? "Reverse (Name2 -> Name)" : "Forward (Name -> Name2)";
		_logger.Information("=== LinkSwitcher: Scan ===");
		_logger.Information("Container: {SearchPath}", searchPath);
		_logger.Information("Direction: {Direction}", direction);
	}

	public void LogDiscoveredPairs(IReadOnlyList<ObjectPair> pairs)
	{
		if (pairs.Count == 0)
		{
			_logger.Warning("No pairs discovered");
			return;
		}

		_logger.Information("Discovered {PairCount} pair(s)", pairs.Count);
	}

	public void LogPairScanResult(PairOperations pairOps, string containerPath)
	{
		var pair = pairOps.Pair;
		_logger.Information("--- {SourceName} / {TargetName} ---", pair.Source.Name, pair.Target.Name);

		if (pairOps.StructureWarnings.Count == 0)
		{
			_logger.Information("  Structure: OK");
		}
		else
		{
			_logger.Warning("  Structure: MISMATCH");
			foreach (var warning in pairOps.StructureWarnings)
			{
				_logger.Warning("    {Warning}", warning);
			}
		}

		if (pairOps.Operations.Count == 0)
		{
			_logger.Information("  Links: none");
			return;
		}

		var columnWidths = ComputeColumnWidths(pairOps.Operations, containerPath);

		_logger.Information("  Links: {LinkCount}", pairOps.Operations.Count);
		foreach (var operation in pairOps.Operations)
		{
			_logger.Information("    {Line}", FormatOperationLine(operation, containerPath, columnWidths));
		}
	}

	public void LogPairCollectionFailure(ObjectPair pair, string reason)
	{
		_logger.Information("--- {SourceName} / {TargetName} ---", pair.Source.Name, pair.Target.Name);
		_logger.Error("  Link collection failed: {Reason}", reason);
	}

	public void LogNoLinksFound()
	{
		_logger.Warning("No links to transfer across all pairs");
	}

	public void LogScanSummary(int pairCount, int totalLinks)
	{
		_logger.Information("Total: {TotalLinks} link(s) across {PairCount} pair(s) queued", totalLinks, pairCount);
	}

	public void LogExecutionHeader()
	{
		_logger.Information("=== LinkSwitcher: Execution ===");
	}

	public void LogPairExecutionHeader(ObjectPair pair)
	{
		_logger.Information("--- {SourceName} / {TargetName} ---", pair.Source.Name, pair.Target.Name);
	}

	public void LogOperationSuccess(LinkOperation operation, string containerPath, ColumnWidths columnWidths)
	{
		_logger.Information("  [OK]  {Line}", FormatOperationLine(operation, containerPath, columnWidths));
	}

	public void LogOperationFailure(LinkOperation operation, string containerPath, ColumnWidths columnWidths, string error)
	{
		_logger.Error("  [ERR] {Line}: {Error}", FormatOperationLine(operation, containerPath, columnWidths), error);
	}

	public void LogExecutionSummary(int totalCount, int successCount, int failureCount)
	{
		if (failureCount == 0)
		{
			_logger.Information("Result: {SuccessCount}/{TotalCount} OK", successCount, totalCount);
		}
		else
		{
			_logger.Warning("Result: {SuccessCount}/{TotalCount} OK, {FailureCount} error(s)",
				successCount, totalCount, failureCount);
		}
	}

	public void LogCancellation()
	{
		_logger.Information("=== LinkSwitcher: Cancelled ===");
	}

	public void LogError(string message)
	{
		_logger.Error("ERROR: {ErrorMessage}", message);
	}

	public static ColumnWidths ComputeColumnWidths(IReadOnlyList<LinkOperation> operations, string containerPath)
	{
		if (operations.Count == 0)
		{
			return new ColumnWidths(0, 0);
		}

		var maxSource = operations.Max(op => StripPrefix(op.SourcePinPath, containerPath).Length);
		var maxTarget = operations.Max(op => StripPrefix(op.TargetPinPath, containerPath).Length);
		return new ColumnWidths(maxSource, maxTarget);
	}

	private static string FormatOperationLine(LinkOperation operation, string containerPath, ColumnWidths widths)
	{
		var tag = operation.IsIConnect ? FeedbackTag : DirectTag;
		var sourceRelative = StripPrefix(operation.SourcePinPath, containerPath);
		var targetRelative = StripPrefix(operation.TargetPinPath, containerPath);

		var paddedSource = sourceRelative.PadRight(widths.SourceWidth);
		var paddedTarget = targetRelative.PadRight(widths.TargetWidth);

		return $"{tag} {paddedSource} -> {paddedTarget} : {operation.ExternalPinPath}";
	}

	private static string StripPrefix(string fullPath, string rootPath)
	{
		if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) &&
			fullPath.Length > rootPath.Length &&
			fullPath[rootPath.Length] == '.')
		{
			return fullPath.Substring(rootPath.Length + 1);
		}

		return fullPath;
	}
}

public readonly record struct ColumnWidths(int SourceWidth, int TargetWidth);
