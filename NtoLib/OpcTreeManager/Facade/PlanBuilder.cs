using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;

using Serilog;

namespace NtoLib.OpcTreeManager.Facade;

/// <summary>
/// Pure helper that constructs a <see cref="RebuildPlan"/> from already-resolved
/// inputs. Contains no vendor COM calls and is therefore directly testable.
/// </summary>
internal static class PlanBuilder
{
	/// <summary>
	/// Builds a rebuild plan from the resolved config, snapshot and current top-level
	/// group contents.
	/// </summary>
	/// <param name="opcFbPath">Absolute SCADA path to the OPC UA FB node.</param>
	/// <param name="groupName">Name of the OPC UA group.</param>
	/// <param name="targetProject">Target project key used to select node names from <paramref name="config"/>.</param>
	/// <param name="config">Already-loaded OPC config.</param>
	/// <param name="snapshot">Already-loaded node snapshot keyed by top-level node name.</param>
	/// <param name="currentTopLevelNames">Names currently present as direct children of the group.</param>
	/// <param name="logger">Optional logger for informational messages.</param>
	/// <returns>
	/// <c>Ok(null)</c> when no operations are required (short-circuit);
	/// <c>Ok(plan)</c> when a rebuild plan is produced;
	/// <c>Fail</c> when the target project is not present in config, has no nodes, does not resolve
	/// against the snapshot, or when the group is empty and no desired node is in the snapshot.
	/// </returns>
	public static Result<RebuildPlan?> Build(
		string opcFbPath,
		string groupName,
		string targetProject,
		OpcConfig config,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		IReadOnlyList<string> currentTopLevelNames,
		ILogger? logger = null)
	{
		if (!config.Projects.TryGetValue(targetProject, out var nodeNames) || nodeNames == null || nodeNames.Count == 0)
		{
			return Result.Fail(
				$"Project '{targetProject}' not found in config or has no nodes; "
				+ $"projects present: [{string.Join(", ", config.Projects.Keys)}].");
		}

		var desiredTree = nodeNames
			.Where(n => n != null && !string.IsNullOrEmpty(n.Name))
			.ToList();

		if (desiredTree.Count == 0)
		{
			return Result.Fail(
				$"Project '{targetProject}' has no valid nodes after filtering null/empty entries. "
				+ "Refusing to build a destructive plan that would clear the whole group.");
		}

		if (CurrentContentsAlreadyMatch(desiredTree, currentTopLevelNames))
		{
			logger?.Information(
				"Group '{GroupName}' top-level node names already match project '{TargetProject}'; no rebuild",
				groupName, targetProject);

			return Result.Ok<RebuildPlan?>(null);
		}

		var resolvable = CheckDesiredTreeResolves(desiredTree, snapshot, targetProject, groupName);

		if (resolvable.IsFailed)
		{
			return resolvable;
		}

		var missingFromSnapshot = FindNamesMissingFromSnapshot(desiredTree, snapshot);

		if (currentTopLevelNames.Count == 0 && missingFromSnapshot.Count == desiredTree.Count)
		{
			return Result.Fail(
				$"Group '{groupName}' is empty and no desired node of project '{targetProject}' is "
				+ "present in the snapshot; refusing to build a plan that would restore nothing. "
				+ "Re-capture with ExecuteSnapshot or drop the nodes from config.yaml.");
		}

		LogPlan(groupName, targetProject, desiredTree, snapshot, currentTopLevelNames, missingFromSnapshot, logger);

		return Result.Ok<RebuildPlan?>(new RebuildPlan(opcFbPath, groupName, desiredTree, snapshot));
	}

	private static bool CurrentContentsAlreadyMatch(
		IReadOnlyList<NodeSpec> desiredTree,
		IReadOnlyList<string> currentTopLevelNames)
	{
		if (desiredTree.Any(s => s.Children != null))
		{
			return false;
		}

		var desiredSet = new HashSet<string>(
			desiredTree.Select(s => s.Name),
			StringComparer.Ordinal);

		return desiredSet.SetEquals(currentTopLevelNames);
	}

	/// <summary>Fails when any desired node would abort the rebuild half-way through.</summary>
	private static Result CheckDesiredTreeResolves(
		IReadOnlyList<NodeSpec> desiredTree,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		string targetProject,
		string groupName)
	{
		// plan-time guard: the executor throws mid-rebuild after disconnects - see
		// Docs/architecture/architecture.md, "PlanBuilder Pure-Helper Pattern".
		var unresolvable = FindUnresolvableNode(desiredTree, snapshot, groupName);

		return unresolvable == null
			? Result.Ok()
			: Result.Fail(
				$"Desired node '{unresolvable}' for project '{targetProject}' is not present in the "
				+ "snapshot; refusing to build a plan that would abort mid-rebuild. Re-capture with "
				+ "ExecuteSnapshot or drop the node from config.yaml.");
	}

	/// <summary>
	/// An entry whose ScadaItem is null resolves to a null DTO at execute time, so the key alone does
	/// not make the node restorable.
	/// </summary>
	private static List<string> FindNamesMissingFromSnapshot(
		IReadOnlyList<NodeSpec> desiredTree,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot)
	{
		return desiredTree
			.Where(s => !snapshot.TryGetValue(s.Name, out var entry) || entry.ScadaItem == null)
			.Select(s => s.Name)
			.ToList();
	}

	private static void LogPlan(
		string groupName,
		string targetProject,
		IReadOnlyList<NodeSpec> desiredTree,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		IReadOnlyList<string> currentTopLevelNames,
		IReadOnlyList<string> missingFromSnapshot,
		ILogger? logger)
	{
		if (logger == null)
		{
			return;
		}

		var currentNames = new HashSet<string>(currentTopLevelNames, StringComparer.Ordinal);
		var desiredNames = new HashSet<string>(desiredTree.Select(s => s.Name), StringComparer.Ordinal);
		var unrestorableNames = new HashSet<string>(missingFromSnapshot, StringComparer.Ordinal);

		var removeNodes = currentTopLevelNames.Where(n => !desiredNames.Contains(n)).ToList();
		var preserveNodes = desiredTree.Select(s => s.Name).Where(currentNames.Contains).ToList();
		var absentNames = desiredTree.Select(s => s.Name).Where(n => !currentNames.Contains(n)).ToList();
		var constructNodes = absentNames.Where(n => !unrestorableNames.Contains(n)).ToList();

		foreach (var missingName in absentNames.Where(unrestorableNames.Contains))
		{
			logger.Error(
				"Node '{NodeName}' of project '{TargetProject}' will not be restored: not in group "
				+ "'{GroupName}', not in the snapshot; re-capture with ExecuteSnapshot or drop it "
				+ "from config.yaml",
				missingName, targetProject, groupName);
		}

		var linkCount = constructNodes.Sum(n => snapshot[n].Links.Count);

		logger.Information(
			"Plan for group '{GroupName}', project '{TargetProject}': remove {RemoveNodes}; "
			+ "construct {ConstructNodes} ({LinkCount} links under them in the snapshot); "
			+ "preserve {PreserveNodes}",
			groupName, targetProject, removeNodes, constructNodes, linkCount, preserveNodes);
	}

	/// <summary>Mirrors the runtime resolver in <see cref="TreeOperations.PlanExecutor"/>: the top
	/// level through <c>snapshot.TryGetValue(name).ScadaItem</c>, nested nodes by ordinal <c>Name</c>
	/// down <c>Items</c>.</summary>
	private static string? FindUnresolvableNode(
		IReadOnlyList<NodeSpec> desired,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		string groupName)
	{
		foreach (var spec in desired)
		{
			var dto = snapshot.TryGetValue(spec.Name, out var s) ? s.ScadaItem : null;

			// A top-level node absent from the snapshot resolves to a null DTO at runtime and is
			// skipped, never pruned - no throw; the plan line records it.
			if (dto == null || spec.Children == null)
			{
				continue;
			}

			var nested = FindUnresolvableChild(spec.Children, dto, groupName + "." + spec.Name);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	/// <summary>
	/// Nested half of <see cref="FindUnresolvableNode"/>: resolves each child spec by ordinal
	/// <c>Name</c> against <paramref name="parentDto"/>'s <c>Items</c>, mirroring the executor's
	/// <c>childDto.Items.FirstOrDefault(i => i.Name == inner)</c>.
	/// </summary>
	private static string? FindUnresolvableChild(
		IReadOnlyList<NodeSpec> children,
		OpcScadaItemDto parentDto,
		string parentPath)
	{
		foreach (var spec in children)
		{
			var childDto = parentDto.Items.FirstOrDefault(i => i.Name == spec.Name);
			var path = parentPath + "." + spec.Name;

			if (childDto == null)
			{
				return path;
			}

			if (spec.Children != null)
			{
				var deeper = FindUnresolvableChild(spec.Children, childDto, path);
				if (deeper != null)
				{
					return deeper;
				}
			}
		}

		return null;
	}
}
