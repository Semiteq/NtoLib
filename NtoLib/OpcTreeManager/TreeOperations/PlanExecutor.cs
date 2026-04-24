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
	private readonly ILogger _logger;

	public PlanExecutor(IProjectHlp project, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));

		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		_logger = logger.ForContext<PlanExecutor>();
	}

	/// <summary>
	/// Synchronously executes the rebuild: resolves the OPC protocol/group, shrinks the
	/// group, swaps <c>group.Items</c>, calls <c>SynchWihSysTree</c> and
	/// <c>ITreeItemHlp.ApplyChange()</c>, then connects the expand-spec links. Returns
	/// <see cref="Result.Ok"/> on success or a failed <see cref="Result"/> carrying the
	/// protocol/group/tree-item resolution error.
	/// </summary>
	public Result Execute(RebuildPlan plan)
	{
		if (plan == null)
		{
			throw new ArgumentNullException(nameof(plan));
		}

		_logger.Information(
			"Executing plan for OPC FB {OpcFbPath}, group {GroupName} ({Count} expand specs)",
			plan.OpcFbPath, plan.GroupName, plan.ExpandSpecs.Count);

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

		var currentByName = group.Items.ToDictionary(i => i.Name, i => i, StringComparer.Ordinal);
		var desiredNames = new HashSet<string>(plan.DesiredNodeNames, StringComparer.Ordinal);
		var expandByName = plan.ExpandSpecs.ToDictionary(s => s.Name, s => s, StringComparer.Ordinal);

		var toRemoveNames = currentByName.Keys.Where(n => !desiredNames.Contains(n)).ToList();
		var (shrinkTotal, shrinkSuccess, shrinkFail) = DisconnectAll(toRemoveNames, plan.OpcFbPath, groupRelativePath);

		var (newItems, expandedSpecs) = BuildNewItems(plan, currentByName, expandByName);

		SwapGroupItems(group, newItems);

		ResetScadaItemsMap(protocol);
		protocol.SynchWihSysTree();

		var commitResult = CommitStructuralChange(plan.OpcFbPath);
		if (commitResult.IsFailed)
		{
			return commitResult;
		}

		var (expandTotal, expandSuccess, expandFail) = ExecuteExpand(expandedSpecs);

		var linkTotal = shrinkTotal + expandTotal;
		var linkSuccess = shrinkSuccess + expandSuccess;
		var linkFail = shrinkFail + expandFail;

		_logger.Information(
			"Execution complete: shrink={ShrinkCount} expand={ExpandCount}; "
			+ "links total={LinkTotal} ok={LinkSuccess} fail={LinkFail}",
			toRemoveNames.Count, expandedSpecs.Count, linkTotal, linkSuccess, linkFail);

		return Result.Ok();
	}

	/// <summary>
	/// Builds the new <c>group.Items</c> list from the desired node order. Existing items
	/// are preserved verbatim; missing ones are constructed from the expand spec's scada
	/// item DTO. The new items are returned as a local list so that a throw from
	/// <c>ToScadaItem()</c> leaves <c>group.Items</c> untouched — the swap at the call
	/// site is the only mutation. Also returns the subset of <see cref="ExpandSpec"/>s
	/// whose links must be re-connected — <b>only</b> the freshly-constructed items.
	/// Preserved existing items keep their pin wiring through the <c>group.Items</c> swap
	/// and must not be reconnected, because <c>ITreePin.ConnectByName</c> throws
	/// <c>ArgumentOutOfRangeException</c> on a pin slot that is already wired.
	/// </summary>
	private (List<OpcUaScadaItem> NewItems, List<ExpandSpec> ExpandedSpecs) BuildNewItems(
		RebuildPlan plan,
		Dictionary<string, OpcUaScadaItem> currentByName,
		Dictionary<string, ExpandSpec> expandByName)
	{
		var newItems = new List<OpcUaScadaItem>();
		var expandedSpecs = new List<ExpandSpec>();

		foreach (var name in plan.DesiredNodeNames)
		{
			if (currentByName.TryGetValue(name, out var existing))
			{
				newItems.Add(existing);
				_logger.Debug("BuildNewItems — preserved '{NodeName}' (links intact, no reconnect)", name);
			}
			else if (expandByName.TryGetValue(name, out var spec))
			{
				newItems.Add(spec.ScadaItem.ToScadaItem());
				expandedSpecs.Add(spec);
				_logger.Debug(
					"BuildNewItems — newly constructed '{NodeName}' ({LinkCount} links to reconnect)",
					name, spec.Links.Count);
			}
			else
			{
				_logger.Warning(
					"BuildNewItems — node '{NodeName}' not in current group and not in expand specs; skipped.",
					name);
			}
		}

		_logger.Information(
			"BuildNewItems — desired={DesiredCount} preserved={PreservedCount} newlyConstructed={NewlyConstructedCount}",
			plan.DesiredNodeNames.Count,
			newItems.Count - expandedSpecs.Count,
			expandedSpecs.Count);

		return (newItems, expandedSpecs);
	}

	private static void SwapGroupItems(OpcUaScadaItem group, List<OpcUaScadaItem> newItems)
	{
		group.Items.Clear();
		foreach (var item in newItems)
		{
			group.Items.Add(item);
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
		protocol.ScadaRootNode = protocol.ScadaRootNode;
	}

	private (int Total, int Success, int Fail) DisconnectAll(
		IReadOnlyList<string> names, string opcFbPath, string groupRelativePath)
	{
		var total = 0;
		var success = 0;
		var fail = 0;

		foreach (var name in names)
		{
			var fullPath = opcFbPath + "." + groupRelativePath + "." + name;
			var (nodeTotal, nodeSuccess, nodeFail) = DisconnectNodeLinks(fullPath);
			total += nodeTotal;
			success += nodeSuccess;
			fail += nodeFail;
		}

		return (total, success, fail);
	}

	private (int Total, int Success, int Fail) ExecuteExpand(IReadOnlyList<ExpandSpec> specs)
	{
		var total = 0;
		var success = 0;
		var fail = 0;

		foreach (var spec in specs)
		{
			var (t, s, f) = ConnectLinks(spec.Links);
			total += t;
			success += s;
			fail += f;
		}

		return (total, success, fail);
	}

	/// <summary>
	/// Re-enumerates the node subtree at execution time and disconnects all
	/// connections from its pins. Used for nodes that are present in the current
	/// group but absent from <see cref="RebuildPlan.DesiredNodeNames"/> — the plan
	/// does not pre-collect their links, so live re-enumeration is required.
	/// </summary>
	private (int Total, int Success, int Fail) DisconnectNodeLinks(string nodePath)
	{
		var node = _project.SafeItem<ITreeItemHlp>(nodePath);
		if (node == null)
		{
			_logger.Error("Disconnect — node not found: {NodePath}", nodePath);
			return (1, 0, 1);
		}

		var allPins = node.EnumAllChilds(TreeMasks.AllPinKinds, 0);

		var success = 0;
		var fail = 0;

		foreach (var child in allPins)
		{
			if (child is not ITreePinHlp localPin)
			{
				continue;
			}

			var (s, f) = DisconnectPinConnections(localPin);
			success += s;
			fail += f;
		}

		return (success + fail, success, fail);
	}

	private (int Success, int Fail) DisconnectPinConnections(ITreePinHlp localPin)
	{
		var success = 0;
		var fail = 0;

		foreach (var mask in new[]
		{
			EConnectionTypeMask.ctGenericPin,
			EConnectionTypeMask.ctGenericPout,
			EConnectionTypeMask.ctIConnect,
		})
		{
			// Materialise the COM enumerable before iterating to avoid modifying the
			// collection while enumerating it (COM collections are live views).
			var connections = localPin.GetConnections(mask).Cast<ITreePinHlp>().ToList();

			foreach (var externalPin in connections)
			{
				try
				{
					localPin.Disconnect(externalPin);
					_logger.Debug(
						"Disconnected {LocalPin} ← {ExternalPin}",
						localPin.FullName,
						externalPin.FullName);
					success++;
				}
				catch (Exception ex)
				{
					_logger.Error(
						"Disconnect {LocalPin} ← {ExternalPin} — {Message}",
						localPin.FullName,
						externalPin.FullName,
						ex.Message);
					fail++;
				}
			}
		}

		return (success, fail);
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
