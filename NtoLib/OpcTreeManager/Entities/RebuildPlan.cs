using System.Collections.Generic;

namespace NtoLib.OpcTreeManager.Entities;

/// <summary>
/// A materialised plan to rebuild an OPC UA FB group's subtree so it matches
/// the target project's desired shape. <see cref="DesiredTree"/> carries the
/// recursive shape. <see cref="Snapshot"/> is the full deserialized tree.json
/// (keyed by top-level group-child name) from which <see cref="TreeOperations.PlanExecutor"/>
/// resolves newly-constructed nodes on demand by walking the snapshot's nested
/// <c>scadaItem.items</c> down to the requested path.
/// </summary>
public sealed record RebuildPlan(
	string OpcFbPath,
	string GroupName,
	IReadOnlyList<NodeSpec> DesiredTree,
	IReadOnlyDictionary<string, NodeSnapshot> Snapshot);
