using System;
using System.Collections.Generic;
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

namespace NtoLib.OpcTreeManager.Facade;

public sealed class OpcTreeManagerService : IOpcTreeManagerService
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

		_logger = logger.ForContext<OpcTreeManagerService>();
		_planExecutor = new PlanExecutor(project, logger);
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

		if (!config.Projects.TryGetValue(targetProject, out var nodeNames) || nodeNames == null || nodeNames.Count == 0)
		{
			var message = $"Project '{targetProject}' not found in config or has no nodes.";
			_logger.Error("{ErrorMessage}", message);

			return Result.Fail(message);
		}

		_logger.Information(
			"Tree scan begin; OpcFbPath={OpcFbPath} GroupName={GroupName} TargetProject={TargetProject}",
			opcFbPath, groupName, targetProject);

		var groupResult = ResolveGroup(opcFbPath, groupName);

		if (groupResult.IsFailed)
		{
			return LogAndFail(groupResult.Errors);
		}

		var snapshotResult = TreeSnapshotLoader.Load(treeJsonPath);

		if (snapshotResult.IsFailed)
		{
			var reason = File.Exists(treeJsonPath)
				? $"Failed to load snapshot from '{treeJsonPath}': {string.Join("; ", snapshotResult.Errors)}"
				: $"Snapshot file not found at '{treeJsonPath}'.";
			return LogAndFail(new[] { new Error(reason) });
		}

		var snapshot = snapshotResult.Value;

		var desiredNames = nodeNames.Where(n => n != null).Distinct(StringComparer.Ordinal).ToList();
		var desiredSet = new HashSet<string>(desiredNames, StringComparer.Ordinal);
		var currentSet = new HashSet<string>(
			groupResult.Value.Group.Items.Select(i => i.Name),
			StringComparer.Ordinal);

		if (desiredSet.SetEquals(currentSet))
		{
			_logger.Information(
				"No operations required for group '{GroupName}' — current contents already match target project '{TargetProject}'.",
				groupName, targetProject);
			return Result.Ok();
		}

		var expandSpecs = BuildExpandSpecs(desiredSet, snapshot);

		_logger.Information("Expand specs queued: {Count}", expandSpecs.Count);
		PendingPlan = new RebuildPlan(opcFbPath, groupName, desiredNames, expandSpecs);

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
			_logger.Information("Operation cancelled by user");
			PendingPlan = null;
		}
	}

	public Result<Dictionary<string, NodeSnapshot>> BuildSnapshot(string opcFbPath, string groupName)
	{
		var groupResult = ResolveGroup(opcFbPath, groupName);

		if (groupResult.IsFailed)
		{
			return LogAndFail(groupResult.Errors);
		}

		var (groupItem, groupRelativePath) = groupResult.Value;
		var items = groupItem.Items.AsReadOnly();
		var snapshot = new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal);

		foreach (var item in items)
		{
			var fullPath = JoinPath(opcFbPath, groupRelativePath, item.Name);
			var node = _project.SafeItem<ITreeItemHlp>(fullPath);

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

		_logger.Information(
			"Snapshot built; {NodeCount} nodes scanned from group '{GroupName}'",
			snapshot.Count, groupName);

		return Result.Ok(snapshot);
	}

	public Result CaptureAndWriteSnapshot(string opcFbPath, string groupName, string treeJsonPath)
	{
		var snapshotResult = BuildSnapshot(opcFbPath, groupName);
		if (snapshotResult.IsFailed)
		{
			return Result.Fail(snapshotResult.Errors);
		}

		var writeResult = TreeSnapshotWriter.Write(snapshotResult.Value, treeJsonPath);
		if (writeResult.IsFailed)
		{
			return LogAndFail(writeResult.Errors);
		}

		_logger.Information("Snapshot written to '{Path}'", treeJsonPath);
		return Result.Ok();
	}

	private static IReadOnlyList<ExpandSpec> BuildExpandSpecs(
		HashSet<string> nodeNameSet,
		Dictionary<string, NodeSnapshot> snapshot)
	{
		var specs = new List<ExpandSpec>();

		foreach (var name in nodeNameSet)
		{
			if (!snapshot.TryGetValue(name, out var nodeSnapshot) || nodeSnapshot.ScadaItem == null)
			{
				continue;
			}

			specs.Add(new ExpandSpec(
				Name: name,
				ScadaItem: nodeSnapshot.ScadaItem,
				Links: nodeSnapshot.Links));
		}

		return specs;
	}

	private Result LogAndFail(IEnumerable<IError> errors)
	{
		var message = string.Join("; ", errors);
		_logger.Error("{ErrorMessage}", message);
		return Result.Fail(message);
	}

	private Result<(OpcUaScadaItem Group, string RelativePath)> ResolveGroup(
		string opcFbPath, string groupName)
	{
		var protocolResult = OpcProtocolAccessor.GetProtocol(_project, opcFbPath);
		if (protocolResult.IsFailed)
		{
			return Result.Fail(protocolResult.Errors);
		}

		return OpcProtocolAccessor.FindGroup(protocolResult.Value, groupName);
	}

	private static string JoinPath(string opcFbPath, string groupRelativePath, string nodeName)
	{
		return $"{opcFbPath}.{groupRelativePath}.{nodeName}";
	}
}
