using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.OpcTreeManager.Entities;

using OpcUaClient.Client.Common;
using OpcUaClient.Client.Common.Data;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal sealed class PlanExecutor
{
	private readonly IProjectHlp _project;
	private readonly ISubtreeDisconnector _disconnector;
	private readonly ILogger _logger;

	/// <summary>
	/// <paramref name="project"/> may be <c>null</c> when the instance is used only for
	/// in-memory <see cref="TestApplyDesiredSpec"/> calls from tests.
	/// <see cref="Execute"/> requires a non-null project and guards against misuse.
	/// </summary>
	public PlanExecutor(IProjectHlp project, ISubtreeDisconnector disconnector, ILogger logger)
	{
		_project = project!;
		_disconnector = disconnector ?? throw new ArgumentNullException(nameof(disconnector));

		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		_logger = logger.ForContext<PlanExecutor>();
	}

	/// <summary>
	/// Synchronously executes the rebuild: resolves the OPC protocol/group, applies the
	/// <see cref="RebuildPlan.DesiredTree"/> recursively at every nesting level
	/// (disconnecting removed subtrees, constructing missing ones pruned to the spec,
	/// preserving matches), calls <c>SynchWihSysTree</c> and <c>ITreeItemHlp.ApplyChange()</c>
	/// once at the group level, then reconnects links for all freshly-constructed nodes.
	/// </summary>
	public Result Execute(RebuildPlan plan)
	{
		if (plan == null)
		{
			throw new ArgumentNullException(nameof(plan));
		}

		if (_project == null)
		{
			throw new InvalidOperationException(
				"PlanExecutor.Execute requires a non-null IProjectHlp; this instance was constructed for "
				+ "in-memory test usage only (ApplyDesiredSpec path).");
		}

		_logger.Information(
			"Executing plan for OPC FB {OpcFbPath}, group {GroupName} ({Count} top-level nodes desired)",
			plan.OpcFbPath, plan.GroupName, plan.DesiredTree.Count);

		var protocolResult = OpcProtocolAccessor.GetProtocol(_project, plan.OpcFbPath);

		if (protocolResult.IsFailed)
		{
			return protocolResult.ToResult();
		}

		var protocol = protocolResult.Value;
		var groupResult = OpcProtocolAccessor.FindGroup(protocol, plan.GroupName);

		if (groupResult.IsFailed)
		{
			return groupResult.ToResult();
		}

		var (group, groupRelativePath) = groupResult.Value;
		var groupPath = plan.OpcFbPath + "." + groupRelativePath;

		var context = new RebuildContext();

		// Top-level: each desired child resolves through plan.Snapshot (keyed by
		// top-level name). Links for each top-level subtree live in the same
		// snapshot entry's Links list and are inherited by deeper recursive calls.
		ApplyDesiredSpec(
			container: group,
			desired: plan.DesiredTree,
			containerPath: groupPath,
			resolveChild: name => plan.Snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			context: context);

		ResetScadaItemsMap(protocol);
		protocol.SynchWihSysTree();

		var commitResult = CommitStructuralChange(plan.OpcFbPath);
		if (commitResult.IsFailed)
		{
			return commitResult;
		}

		var (expandTotal, expandSuccess, expandFail) = ExecuteExpand(context.Constructions);

		var linkTotal = context.ShrinkTotal + expandTotal;
		var linkSuccess = context.ShrinkSuccess + expandSuccess;
		var linkFail = context.ShrinkFail + expandFail;

		_logger.Information(
			"Execution complete: shrink={ShrinkCount} expand={ExpandCount}; "
			+ "links total={LinkTotal} ok={LinkSuccess} fail={LinkFail}",
			context.ShrinkCount, context.Constructions.Count, linkTotal, linkSuccess, linkFail);

		return Result.Ok();
	}

	/// <summary>
	/// Test entry point: directly invokes <see cref="ApplyDesiredSpec"/> against an in-memory
	/// container and snapshot, returning the produced constructions and shrink count for assertion.
	/// </summary>
	internal void TestApplyDesiredSpec(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		out List<Construction> constructions,
		out int shrinkCount)
	{
		var context = new RebuildContext();
		ApplyDesiredSpec(
			container: container,
			desired: desired,
			containerPath: containerPath,
			resolveChild: name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			context: context);
		constructions = context.Constructions;
		shrinkCount = context.ShrinkCount;
	}

	internal readonly record struct Construction(string Path, IReadOnlyList<LinkEntry> Links);

	private sealed class RebuildContext
	{
		public List<Construction> Constructions { get; } = new();
		public int ShrinkCount { get; set; }
		public int ShrinkTotal { get; set; }
		public int ShrinkSuccess { get; set; }
		public int ShrinkFail { get; set; }
	}

	/// <summary>
	/// Rebuilds <paramref name="container"/>'s <c>Items</c> to match <paramref name="desired"/>.
	/// Missing items are constructed from the snapshot DTO returned by
	/// <paramref name="resolveChild"/> (pruned to match the spec's children),
	/// existing items whose names match are preserved. Removed items are live-disconnected
	/// and then dropped. Recurses into each preserved item whose spec has non-null
	/// <c>Children</c>, carrying the resolved DTO downwards so deep constructions can
	/// walk the same snapshot subtree.
	/// </summary>
	private void ApplyDesiredSpec(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		Func<string, (OpcScadaItemDto? Dto, IReadOnlyList<LinkEntry> Links)> resolveChild,
		RebuildContext context)
	{
		var currentByName = container.Items.ToDictionary(i => i.Name, i => i, StringComparer.Ordinal);
		var desiredNames = new HashSet<string>(desired.Select(s => s.Name), StringComparer.Ordinal);

		foreach (var name in currentByName.Keys.Where(n => !desiredNames.Contains(n)).ToList())
		{
			var (total, success, fail) = _disconnector.DisconnectSubtree(containerPath + "." + name);
			context.ShrinkCount++;
			context.ShrinkTotal += total;
			context.ShrinkSuccess += success;
			context.ShrinkFail += fail;
		}

		var newItems = new List<OpcUaScadaItem>(desired.Count);
		var preservedCount = 0;
		var constructedCount = 0;

		foreach (var spec in desired)
		{
			var childPath = containerPath + "." + spec.Name;
			var (childDto, childLinks) = resolveChild(spec.Name);

			if (currentByName.TryGetValue(spec.Name, out var existing))
			{
				newItems.Add(existing);
				preservedCount++;
				_logger.Debug("BuildNewItems — preserved '{NodePath}' (links intact, no reconnect)", childPath);

				if (spec.Children != null)
				{
					ApplyDesiredSpec(
						existing,
						spec.Children,
						childPath,
						inner => childDto != null
							? (childDto.Items.FirstOrDefault(i => i.Name == inner), childLinks)
							: (null, Array.Empty<LinkEntry>()),
						context);
				}

				continue;
			}

			if (childDto == null)
			{
				_logger.Warning(
					"BuildNewItems — node '{NodePath}' not in current container and not in snapshot; skipped.",
					childPath);
				continue;
			}

			var constructed = childDto.ToScadaItemPruned(spec);
			newItems.Add(constructed);
			constructedCount++;

			var keptPaths = EnumerateSubtreeNodePaths(constructed, childPath).ToArray();
			var filteredLinks = LinkCollector.FilterForSubtree(childLinks, keptPaths);
			context.Constructions.Add(new Construction(childPath, filteredLinks));

			_logger.Debug(
				"BuildNewItems — newly constructed '{NodePath}' ({LinkCount} links to reconnect)",
				childPath, filteredLinks.Count);
		}

		_logger.Information(
			"BuildNewItems at '{ContainerPath}' — desired={DesiredCount} preserved={PreservedCount} newlyConstructed={NewlyConstructedCount}",
			containerPath, desired.Count, preservedCount, constructedCount);

		SwapContainerItems(container, newItems);
	}

	private static IEnumerable<string> EnumerateSubtreeNodePaths(OpcUaScadaItem item, string itemPath)
	{
		yield return itemPath;

		foreach (var child in item.Items)
		{
			foreach (var descendantPath in EnumerateSubtreeNodePaths(child, itemPath + "." + child.Name))
			{
				yield return descendantPath;
			}
		}
	}

	private static void SwapContainerItems(OpcUaScadaItem container, List<OpcUaScadaItem> newItems)
	{
		container.Items.Clear();
		foreach (var item in newItems)
		{
			container.Items.Add(item);
		}
	}

	/// <summary>
	/// Commits the structural change into the project's native name registry so that
	/// vavobj's ConnectByName can resolve the new pin slots. Matches the vendor UI's
	/// OpcUaGroupPropPageWindow.ApplyChanges() and the working scripts/OpcGroup example,
	/// which is the only officially-supported template for dynamic add-and-connect.
	/// </summary>
	private Result CommitStructuralChange(string opcFbPath)
	{
		var opcFbItem = _project.SafeItem<ITreeItemHlp>(opcFbPath);
		if (opcFbItem == null)
		{
			return Result.Fail($"OPC FB tree item not found for ApplyChange: {opcFbPath}");
		}

		opcFbItem.ApplyChange();
		return Result.Ok();
	}

	/// <summary>
	/// The <c>ScadaRootNode</c> setter has a side effect: it clears the internal
	/// <c>_opcUaScadaItemsMap</c> so that the next <c>SynchWihSysTree</c> call re-reads
	/// from the updated <c>Items</c> list. Assigning the property to its own value is
	/// the only public way to trigger that reset.
	/// </summary>
	private static void ResetScadaItemsMap(OpcUaProtocol protocol)
	{
		// Self-assignment is intentional — the setter clears the internal scada-items map.
		// Using a temporary makes the intent explicit to humans and analyzers.
		var root = protocol.ScadaRootNode;
#pragma warning disable CA2245
		protocol.ScadaRootNode = root;
#pragma warning restore CA2245
	}

	private (int Total, int Success, int Fail) ExecuteExpand(IReadOnlyList<Construction> constructions)
	{
		var total = 0;
		var success = 0;
		var fail = 0;

		foreach (var construction in constructions)
		{
			var (t, s, f) = ConnectLinks(construction.Links);
			total += t;
			success += s;
			fail += f;
		}

		return (total, success, fail);
	}

	/// <summary>
	/// Connects the external pins listed in <paramref name="links"/> back to the local OPC
	/// pins. Each <see cref="LinkEntry"/> carries both <see cref="LinkEntry.LocalPinPath"/>
	/// and <see cref="LinkEntry.ExternalPinPath"/>.
	/// </summary>
	private (int Total, int Success, int Fail) ConnectLinks(IReadOnlyList<LinkEntry> links)
	{
		var success = 0;
		var fail = 0;

		foreach (var link in links)
		{
			if (TryConnectLink(link))
			{
				success++;
			}
			else
			{
				fail++;
			}
		}

		return (success + fail, success, fail);
	}

	private bool TryConnectLink(LinkEntry link)
	{
		var localPin = _project.SafeItem<ITreePinHlp>(link.LocalPinPath);
		if (localPin == null)
		{
			_logger.Error("Connect — local pin not found: {Path}", link.LocalPinPath);
			return false;
		}

		var externalPin = _project.SafeItem<ITreePinHlp>(link.ExternalPinPath);
		if (externalPin == null)
		{
			_logger.Error("Connect — external pin not found: {Path}", link.ExternalPinPath);
			return false;
		}

		try
		{
			// Direct wires use the no-arg Connect overload on purpose — see
			// Docs/KnownIssues/05-opc-command-pin-connect-overload.md.
			if (link.LinkType == LinkTypes.IConnect)
			{
				localPin.Connect(externalPin, EConnectionType.ctIConnect);
			}
			else if (link.LinkType == LinkTypes.DirectPin)
			{
				localPin.Connect(externalPin);
			}
			else if (link.LinkType == LinkTypes.DirectPout)
			{
				externalPin.Connect(localPin);
			}
			else
			{
				_logger.Error(
					"Connect {LocalPin} ↔ {ExternalPin} — unknown link type '{LinkType}'",
					link.LocalPinPath,
					link.ExternalPinPath,
					link.LinkType);
				return false;
			}

			_logger.Debug(
				"Connected {LocalPin} ↔ {ExternalPin}",
				link.LocalPinPath,
				link.ExternalPinPath);
			return true;
		}
		catch (Exception ex)
		{
			_logger.Error(
				"Connect {LocalPin} ↔ {ExternalPin} — {Message}",
				link.LocalPinPath,
				link.ExternalPinPath,
				ex.Message);
			return false;
		}
	}
}
