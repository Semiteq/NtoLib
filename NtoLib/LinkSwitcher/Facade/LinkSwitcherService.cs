using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;

using NtoLib.LinkSwitcher.Entities;
using NtoLib.LinkSwitcher.Logging;
using NtoLib.LinkSwitcher.TreeOperations;

using Serilog;

namespace NtoLib.LinkSwitcher.Facade;

public sealed class LinkSwitcherService : ILinkSwitcherService
{
	private readonly IProjectHlp _project;
	private readonly PairDiscovery _pairDiscovery;
	private readonly SwitchLogger _switchLogger;

	public bool HasPendingTask => PendingPlan != null;
	public SwitchPlan? PendingPlan { get; private set; }

	public LinkSwitcherService(IProjectHlp project, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));

		_pairDiscovery = new PairDiscovery(project);
		_switchLogger = new SwitchLogger(logger);
	}

	public Result<SwitchPlan> ScanAndValidate(string sourcePath, string targetPath, bool reverse)
	{
		PendingPlan = null;
		_switchLogger.LogScanHeader(sourcePath, targetPath, reverse);

		var pairsResult = _pairDiscovery.FindPairs(sourcePath, targetPath);
		if (pairsResult.IsFailed)
		{
			var errorMessage = string.Join("; ", pairsResult.Errors);
			_switchLogger.LogError(errorMessage);
			return Result.Fail(errorMessage);
		}

		var pairs = pairsResult.Value;
		_switchLogger.LogDiscoveredPairs(pairs);

		if (pairs.Count == 0)
		{
			return Result.Fail("No matching pairs found between source and target containers.");
		}

		var pairResults = new List<PairOperations>();

		foreach (var pair in pairs)
		{
			var structureWarnings = StructureValidator.FindDifferences(pair);

			var collectResult = LinkCollector.CollectOperations(pair, reverse);
			if (collectResult.IsFailed)
			{
				var reason = string.Join("; ", collectResult.Errors);
				_switchLogger.LogPairCollectionFailure(pair, reason);
				continue;
			}

			var pairOps = new PairOperations(pair, collectResult.Value, structureWarnings);
			pairResults.Add(pairOps);
			_switchLogger.LogPairScanResult(pairOps);
		}

		var totalLinks = pairResults.Sum(p => p.Operations.Count);
		if (totalLinks == 0)
		{
			_switchLogger.LogNoLinksFound();
			return Result.Fail("No links to transfer across all pairs.");
		}

		var plan = new SwitchPlan(pairResults, reverse, sourcePath, targetPath);
		_switchLogger.LogScanSummary(pairResults.Count, totalLinks);

		PendingPlan = plan;
		return Result.Ok(plan);
	}

	public Result Execute(SwitchPlan plan)
	{
		var totalOperations = plan.PairResults.Sum(p => p.Operations.Count);
		if (totalOperations == 0)
		{
			_switchLogger.LogExecutionHeader();
			_switchLogger.LogExecutionSummary(0, 0, 0);
			PendingPlan = null;
			return Result.Ok();
		}

		var linkExecutor = new LinkExecutor(_project, _switchLogger);
		var result = linkExecutor.Execute(plan.PairResults, plan.SourcePath, plan.TargetPath);

		PendingPlan = null;
		return result;
	}

	public void Cancel()
	{
		if (PendingPlan != null)
		{
			_switchLogger.LogCancellation();
			PendingPlan = null;
		}
	}
}
