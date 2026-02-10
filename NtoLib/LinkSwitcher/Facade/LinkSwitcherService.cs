using System;
using System.Collections.Generic;

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
	private readonly StructureValidator _structureValidator;
	private readonly LinkCollector _linkCollector;
	private readonly SwitchLogger _switchLogger;

	private SwitchPlan? _pendingPlan;

	public bool HasPendingTask => _pendingPlan != null;
	public SwitchPlan? PendingPlan => _pendingPlan;

	public LinkSwitcherService(IProjectHlp project, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));

		_pairDiscovery = new PairDiscovery(project);
		_structureValidator = new StructureValidator();
		_linkCollector = new LinkCollector();
		_switchLogger = new SwitchLogger(logger);
	}

	public Result<SwitchPlan> ScanAndValidate(string searchPath, bool forward)
	{
		_pendingPlan = null;
		_switchLogger.LogScanHeader(searchPath, forward);

		var pairsResult = _pairDiscovery.FindPairs(searchPath);
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
			return Result.Fail("No Name/Name2 pairs found in the specified container.");
		}

		var allOperations = new List<LinkOperation>();

		foreach (var pair in pairs)
		{
			var validationResult = _structureValidator.Validate(pair);
			if (validationResult.IsFailed)
			{
				var reason = string.Join("; ", validationResult.Errors);
				_switchLogger.LogValidationFailure(pair, reason);
				return Result.Fail($"Validation failed for pair '{pair.Name}': {reason}");
			}

			var collectResult = _linkCollector.CollectOperations(pair, forward);
			if (collectResult.IsFailed)
			{
				var reason = string.Join("; ", collectResult.Errors);
				_switchLogger.LogValidationFailure(pair, reason);
				return Result.Fail($"Link collection failed for pair '{pair.Name}': {reason}");
			}

			_switchLogger.LogValidationSuccess(pair, collectResult.Value.Count);
			allOperations.AddRange(collectResult.Value);
		}

		var plan = new SwitchPlan(pairs, allOperations, forward);
		_switchLogger.LogPlan(allOperations);

		_pendingPlan = plan;
		return Result.Ok(plan);
	}

	public Result Execute(SwitchPlan plan)
	{
		if (plan.Operations.Count == 0)
		{
			_switchLogger.LogExecutionHeader();
			_switchLogger.LogExecutionSummary(0, 0, 0);
			_pendingPlan = null;
			return Result.Ok();
		}

		var linkExecutor = new LinkExecutor(_project, _switchLogger);
		var result = linkExecutor.Execute(plan.Operations);

		_pendingPlan = null;
		return result;
	}

	public Result DryRun(SwitchPlan plan)
	{
		_switchLogger.LogDryRunComplete();
		_pendingPlan = null;
		return Result.Ok();
	}

	public void Cancel()
	{
		if (_pendingPlan != null)
		{
			_switchLogger.LogCancellation();
			_pendingPlan = null;
		}
	}
}
