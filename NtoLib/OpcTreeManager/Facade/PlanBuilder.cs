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

		logger?.Information("Top-level desired nodes: {Count}", desiredTree.Count);

		return Result.Ok<RebuildPlan?>(new RebuildPlan(opcFbPath, groupName, desiredTree, snapshot));
	}
}
