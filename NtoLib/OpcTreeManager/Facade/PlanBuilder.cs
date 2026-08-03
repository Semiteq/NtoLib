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
	/// <c>Fail</c> when the target project is not present in config or has no nodes.
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
			return Result.Fail($"Project '{targetProject}' not found in config or has no nodes.");
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

		var desiredSet = new HashSet<string>(
			desiredTree.Select(s => s.Name),
			StringComparer.Ordinal);

		var currentSet = new HashSet<string>(
			currentTopLevelNames,
			StringComparer.Ordinal);

		// Shallow short-circuit: if the top-level names match AND every top-level
		// spec is a leaf (no children), the current contents already satisfy the
		// target project — no need to touch anything.
		var allLeaves = desiredTree.All(s => s.Children == null);

		if (allLeaves && desiredSet.SetEquals(currentSet))
		{
			logger?.Information(
				"No operations required for group '{GroupName}' — current contents already match target project '{TargetProject}'.",
				groupName, targetProject);

			return Result.Ok<RebuildPlan?>(null);
		}

		// Resolvability guard (fixes the mid-rebuild abort): the executor's ToScadaItemPruned
		// throws when a spec node whose DTO resolved against the snapshot lists a child that is
		// absent from that DTO's Items. The throw fires DURING the rebuild — after removed
		// subtrees are already live-disconnected — leaving a half-rebuilt tree. Detect it here
		// so nothing mutates. The whole desired spec is validated (over-approximation:
		// PlanBuilder cannot know which nodes preserve vs construct). A node absent from the
		// snapshot is NOT a failure — it is the safe skip-with-warning / preserve path
		// (TreeReshaper.ApplyDesiredSpec's childDto == null skip branch), so the walk descends
		// only where the DTO resolved.
		var unresolvable = FindUnresolvableNode(desiredTree, snapshot, groupName);
		if (unresolvable != null)
		{
			return Result.Fail(
				$"Desired node '{unresolvable}' for project '{targetProject}' is not present in the "
				+ "snapshot; refusing to build a plan that would abort mid-rebuild.");
		}

		logger?.Information("Top-level desired nodes: {Count}", desiredTree.Count);

		return Result.Ok<RebuildPlan?>(new RebuildPlan(opcFbPath, groupName, desiredTree, snapshot));
	}

	/// <summary>
	/// Walks the desired spec against the snapshot DTO tree and returns the path of the first node
	/// that does not resolve, or <c>null</c> when the whole spec resolves. Mirrors the runtime
	/// resolver in <see cref="TreeOperations.PlanExecutor"/>: the top level resolves through
	/// <c>snapshot.TryGetValue(name).ScadaItem</c>, nested nodes by descending the parent DTO's
	/// <c>Items</c> and matching children by ordinal <c>Name</c>.
	/// </summary>
	private static string? FindUnresolvableNode(
		IReadOnlyList<NodeSpec> desired,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		string groupName)
	{
		foreach (var spec in desired)
		{
			var dto = snapshot.TryGetValue(spec.Name, out var s) ? s.ScadaItem : null;

			// A top-level node absent from the snapshot resolves to a null DTO at runtime and is
			// skipped-with-warning, never pruned — no throw. Only descend where the DTO resolved.
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
