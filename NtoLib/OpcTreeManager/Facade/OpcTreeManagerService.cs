using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using OpcUaClient.Client.Common.Data;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace NtoLib.OpcTreeManager.Facade;

public sealed class OpcTreeManagerService
{
	private readonly IProjectHlp _project;
	private readonly ILogger _logger;
	private readonly PlanExecutor _planExecutor;

	public OpcTreeManagerService(IProjectHlp project, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));

		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		_logger = logger;
		var disconnector = new ProjectSubtreeDisconnector(project, logger);
		_planExecutor = new PlanExecutor(project, disconnector, logger);
	}

	public bool HasPendingTask => PendingPlan != null;
	public RebuildPlan? PendingPlan { get; private set; }

	public Result ScanAndValidate(
		string targetProject,
		string opcFbPath,
		string groupName,
		string treeJsonPath,
		string configYamlPath)
	{
		PendingPlan = null;

		var configResult = OpcConfigLoader.Load(configYamlPath);

		if (configResult.IsFailed)
		{
			return LogAndFail(configResult.Errors);
		}

		var config = configResult.Value;

		var groupResult = ResolveGroup(opcFbPath, groupName);

		if (groupResult.IsFailed)
		{
			return LogAndFail(groupResult.Errors);
		}

		_logger.Information(
			"Group '{GroupName}' resolved at '{GroupRelativePath}' for Execute",
			groupName, groupResult.Value.RelativePath);

		var snapshotResult = TreeSnapshotLoader.Load(treeJsonPath);

		if (snapshotResult.IsFailed)
		{
			var notLoaded = new Error($"Snapshot of group '{groupName}' was not loaded from '{treeJsonPath}'")
				.CausedBy(snapshotResult.Errors);

			return LogAndFail(new[] { notLoaded });
		}

		var snapshot = snapshotResult.Value;

		var droppedLinks = snapshotResult.Successes.OfType<DroppedLinksSuccess>().FirstOrDefault()?.Count ?? 0;

		if (droppedLinks > 0)
		{
			_logger.Warning(
				"Snapshot '{TreeJsonPath}': {DroppedLinkCount} links with a blank pin path ignored",
				treeJsonPath, droppedLinks);
		}

		LogSnapshotLoaded(snapshot, treeJsonPath);

		var currentTopLevelNames = groupResult.Value.Group.Items.Select(i => i.Name).ToList();
		var planResult = PlanBuilder.Build(opcFbPath, groupName, targetProject, config, snapshot, currentTopLevelNames, _logger);

		if (planResult.IsFailed)
		{
			return LogAndFail(planResult.Errors);
		}

		PendingPlan = planResult.Value;

		return Result.Ok();
	}

	public void ExecuteDeferred(Logger? logger)
	{
		var plan = PendingPlan;

		if (plan == null)
		{
			logger?.Dispose();
			return;
		}

		DeferredExecutor.Post(_planExecutor, plan, logger, _project, onFinished: () => PendingPlan = null);
	}

	public void Cancel()
	{
		if (PendingPlan != null)
		{
			_logger.Information("Pending rebuild of group '{GroupName}' cancelled", PendingPlan.GroupName);
			PendingPlan = null;
		}
	}

	private Result<Dictionary<string, NodeSnapshot>> BuildSnapshot(string opcFbPath, string groupName)
	{
		var groupResult = ResolveGroup(opcFbPath, groupName);

		if (groupResult.IsFailed)
		{
			return LogAndFail(groupResult.Errors);
		}

		var (groupItem, groupRelativePath) = groupResult.Value;

		_logger.Information(
			"Group '{GroupName}' resolved at '{GroupRelativePath}' for ExecuteSnapshot",
			groupName, groupRelativePath);

		var items = groupItem.Items.AsReadOnly();
		var snapshot = new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal);

		foreach (var item in items)
		{
			var fullPath = JoinPath(opcFbPath, groupRelativePath, item.Name);
			var node = _project.SafeItem<ITreeItemHlp>(fullPath);

			if (node == null)
			{
				_logger.Error(
					"Node '{NodeName}' captured with no links: '{NodePath}' did not resolve in the "
					+ "project; check OpcFbPath",
					item.Name, fullPath);
			}

			var links = node != null
				? LinkCollector.CollectAllLinks(node, _logger)
				: Array.Empty<LinkEntry>();

			var nodeSnapshot = new NodeSnapshot
			{
				Links = links,
				ScadaItem = OpcScadaItemDto.FromScadaItem(item),
			};

			snapshot[item.Name] = nodeSnapshot;
		}

		return Result.Ok(snapshot);
	}

	public Result CaptureAndWriteSnapshot(string opcFbPath, string groupName, string treeJsonPath)
	{
		var snapshotResult = BuildSnapshot(opcFbPath, groupName);
		if (snapshotResult.IsFailed)
		{
			return Result.Fail(snapshotResult.Errors);
		}

		var snapshot = snapshotResult.Value;
		var writeResult = TreeSnapshotWriter.Write(snapshot, treeJsonPath);

		if (writeResult.IsFailed)
		{
			var notWritten = new Error($"Snapshot of group '{groupName}' was not written to '{treeJsonPath}'")
				.CausedBy(writeResult.Errors);

			return LogAndFail(new[] { notWritten });
		}

		LogSnapshotWritten(groupName, treeJsonPath, snapshot, _logger);

		return Result.Ok();
	}

	/// <summary>Terminal record of a capture: the count of nodes written with no links sets the level,
	/// and deliberately not the <c>Failed</c> pin (Docs/opc-tree-manager.md section 8.4).</summary>
	internal static void LogSnapshotWritten(
		string groupName,
		string treeJsonPath,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		ILogger logger)
	{
		var nodesWithoutLinks = snapshot.Values.Count(node => node.Links.Count == 0);

		var nextRebuild = nodesWithoutLinks > 0
			? "; the next rebuild constructs those nodes unwired"
			: string.Empty;

		logger.Write(
			nodesWithoutLinks > 0 ? LogEventLevel.Error : LogEventLevel.Information,
			"Snapshot of group '{GroupName}' written to '{TreeJsonPath}': {NodeCount} nodes, "
			+ "{LinkCount} links, {NodesWithoutLinks} without links" + nextRebuild,
			groupName,
			treeJsonPath,
			snapshot.Count,
			CountLinks(snapshot),
			nodesWithoutLinks);
	}

	private Result LogAndFail(IEnumerable<IError> errors)
	{
		var errorList = errors.ToList();
		var flattened = Flatten(errorList).ToList();
		var message = string.Join("; ", flattened.Select(e => e.Message));
		var exception = flattened.OfType<ExceptionalError>().FirstOrDefault()?.Exception;

		if (exception == null)
		{
			_logger.Error("{ErrorMessage}", message);
		}
		else
		{
			_logger.Error(exception, "{ErrorMessage}", message);
		}

		return Result.Fail(errorList);
	}

	/// <summary>Walks an error and its CausedBy chain so every cause reaches the log line.</summary>
	internal static IEnumerable<IError> Flatten(IEnumerable<IError> errors)
	{
		foreach (var error in errors)
		{
			yield return error;

			foreach (var nested in Flatten(error.Reasons.OfType<IError>()))
			{
				yield return nested;
			}
		}
	}

	private void LogSnapshotLoaded(Dictionary<string, NodeSnapshot> snapshot, string treeJsonPath)
	{
		_logger.Information(
			"Snapshot '{TreeJsonPath}' loaded: {NodeCount} nodes, {LinkCount} links, "
			+ "file written {SnapshotWrittenAt}",
			treeJsonPath, snapshot.Count, CountLinks(snapshot), DescribeWriteTime(treeJsonPath));
	}

	/// <summary>Never throws: <c>ScanAndValidate</c> runs out of <c>UpdateData</c> unguarded, so a date
	/// the file system refuses and the 1601 sentinel it returns instead both read as unknown.</summary>
	private static string DescribeWriteTime(string path)
	{
		try
		{
			var writtenAt = File.GetLastWriteTime(path);

			return writtenAt.Year <= 1601
				? "unknown"
				: writtenAt.ToString("O", CultureInfo.InvariantCulture);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
			or NotSupportedException)
		{
			return "unknown";
		}
	}

	private static int CountLinks(IReadOnlyDictionary<string, NodeSnapshot> snapshot)
	{
		return snapshot.Values.Sum(node => node.Links.Count);
	}

	private Result<(OpcUaScadaItem Group, string RelativePath)> ResolveGroup(
		string opcFbPath, string groupName)
	{
		var protocolResult = OpcProtocolAccessor.GetProtocol(_project, opcFbPath);

		return protocolResult.IsFailed
			? Result.Fail(protocolResult.Errors)
			: OpcProtocolAccessor.FindGroup(protocolResult.Value, groupName);
	}

	private static string JoinPath(string opcFbPath, string groupRelativePath, string nodeName)
	{
		return $"{opcFbPath}.{groupRelativePath}.{nodeName}";
	}
}
