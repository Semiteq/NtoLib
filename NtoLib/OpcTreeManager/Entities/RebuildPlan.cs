using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

/// <summary>
/// A plan to rebuild an OPC UA FB group's subtree into the target project's shape.
/// <see cref="DesiredTree"/> carries that shape; <see cref="Snapshot"/> is tree.json keyed by
/// top-level group-child name, which <see cref="TreeOperations.PlanExecutor"/> walks on demand.
/// </summary>
public sealed record RebuildPlan(
	string OpcFbPath,
	string GroupName,
	IReadOnlyList<NodeSpec> DesiredTree,
	IReadOnlyDictionary<string, NodeSnapshot> Snapshot);
