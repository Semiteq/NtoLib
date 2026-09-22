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
using Serilog.Events;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal sealed class PlanExecutor
{
	private readonly IProjectHlp _project;
	private readonly ISubtreeDisconnector _disconnector;
	private readonly ILogger _logger;

	public PlanExecutor(IProjectHlp project, ISubtreeDisconnector disconnector, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
		_disconnector = disconnector ?? throw new ArgumentNullException(nameof(disconnector));

		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		_logger = logger;
	}

	/// <summary>
	/// Synchronously executes the rebuild: resolves the OPC protocol/group, applies the
	/// <see cref="RebuildPlan.DesiredTree"/> recursively at every nesting level
	/// (disconnecting removed subtrees, constructing missing ones pruned to the spec,
	/// preserving matches), calls <c>SynchWihSysTree</c> and <c>ITreeItemHlp.ApplyChange()</c>
	/// once at the group level, then connects every link in-pass on this single
	/// tick — direct links first, iconnect last. Runs once, after the host clears
	/// <see cref="IProjectHlp.InRuntime"/> (the wait lives in <c>DeferredExecutor</c>).
	/// Success is judged by the SCADA tree on reload, not in code: an in-code read-back via
	/// <c>GetConnections</c> is blind after the structural commit (a connected link reads back
	/// as absent), so the summary reports only what is known — connects issued and how many threw.
	/// See <c>Docs/known_issues/11</c>.
	/// </summary>
	public Result Execute(RebuildPlan plan, Action onTreeMutationStarting)
	{
		if (plan == null)
		{
			throw new ArgumentNullException(nameof(plan));
		}

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

		// Raised by the reshape at its first write, from any depth and from inside the disconnector.
		var mutationFlagged = false;

		void FlagTreeMutation()
		{
			if (mutationFlagged)
			{
				return;
			}

			mutationFlagged = true;
			onTreeMutationStarting();
		}

		// Top-level: each desired child resolves through plan.Snapshot (keyed by
		// top-level name). Links for each top-level subtree live in the same
		// snapshot entry's Links list and are inherited by deeper recursive calls.
		var reshape = TreeReshaper.Reshape(
			container: group,
			desired: plan.DesiredTree,
			containerPath: groupPath,
			resolveChild: name => plan.Snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			disconnector: _disconnector,
			onTreeMutationStarting: FlagTreeMutation,
			logger: _logger);

		ResetScadaItemsMap(protocol);
		protocol.SynchWihSysTree();

		// Resolved after SynchWihSysTree, not before the reshape: the vendor UI's
		// OpcUaGroupPropPageWindow.ApplyChanges() takes the handle in that order, and ApplyChange commits
		// the new pin slots into the native name registry vavobj's ConnectByName resolves against.
		var opcFbItem = _project.SafeItem<ITreeItemHlp>(plan.OpcFbPath);
		if (opcFbItem == null)
		{
			return Result.Fail($"OPC FB tree item not found for ApplyChange: {plan.OpcFbPath}");
		}

		opcFbItem.ApplyChange();

		var (commands, linksUnresolved) = BuildCommands(
			reshape.Constructions,
			path => _project.SafeItem<ITreePinHlp>(path),
			_logger);

		var orderedCommands = CommandOrdering.OrderCommandsDirectFirst(commands);

		var (connectsIssued, connectsThrew) = ConnectRunner.ConnectAll(orderedCommands, _logger);

		LogRebuildFinished(
			groupName: plan.GroupName,
			reshape: reshape,
			connectsIssued: connectsIssued,
			connectsThrew: connectsThrew,
			linksUnresolved: linksUnresolved,
			logger: _logger);

		return Result.Ok();
	}

	internal static void LogRebuildFinished(
		string groupName,
		ReshapeResult reshape,
		int connectsIssued,
		int connectsThrew,
		int linksUnresolved,
		ILogger logger)
	{
		var constructionsWithoutLinks = reshape.Constructions.Count(c => c.Links.Count == 0);

		var wiringIncomplete = linksUnresolved > 0 || constructionsWithoutLinks > 0;

		var structureIncomplete = connectsThrew > 0
			|| reshape.DisconnectsThrew > 0
			|| reshape.NodesMissing > 0
			|| reshape.NodesNotRestored > 0;

		var failed = wiringIncomplete || structureIncomplete;
		var verdict = failed ? "with failures" : "without failures";

		// The destructive half can succeed while the connects fail, and Execute still returns Ok, so
		// this line is the only place that states what is on screen and what the operator does with it.
		var nextStep = (wiringIncomplete, structureIncomplete) switch
		{
			(false, false) => string.Empty,
			(true, false) => "; the nodes are in the tree with links missing, close the project "
				+ "without saving, then re-capture the snapshot or repair the consumers",
			_ => "; the tree is partially rebuilt, close the project without saving",
		};

		logger.Write(
			failed ? LogEventLevel.Error : LogEventLevel.Information,
			"Rebuild of group '{GroupName}' finished " + verdict + ": removed={RemovedCount} "
			+ "constructed={ConstructedCount} noLinks={ConstructionsWithoutLinks} "
			+ "notRestored={NodesNotRestored}; disconnects issued={DisconnectsIssued} "
			+ "threw={DisconnectsThrew} nodesMissing={NodesMissing}; connects "
			+ "issued={ConnectsIssued} threw={ConnectsThrew} unresolved={LinksUnresolved}"
			+ nextStep,
			groupName,
			reshape.RemovedCount,
			reshape.Constructions.Count,
			constructionsWithoutLinks,
			reshape.NodesNotRestored,
			reshape.DisconnectsIssued,
			reshape.DisconnectsThrew,
			reshape.NodesMissing,
			connectsIssued,
			connectsThrew,
			linksUnresolved);
	}

	/// <summary>
	/// The <c>ScadaRootNode</c> setter has a side effect: it clears the internal
	/// <c>_opcUaScadaItemsMap</c> so that the next <c>SynchWihSysTree</c> call re-reads
	/// from the updated <c>Items</c> list. Assigning the property to its own value is
	/// the only public way to trigger that reset.
	/// </summary>
	private static void ResetScadaItemsMap(OpcUaProtocol protocol)
	{
		// Self-assignment triggers the setter's map-reset side effect (see summary); CA2245 flags it.
		var root = protocol.ScadaRootNode;
#pragma warning disable CA2245
		protocol.ScadaRootNode = root;
#pragma warning restore CA2245
	}

	/// <summary>
	/// Builds a <see cref="ConnectCommand"/> for every resolvable link across all constructions. Links
	/// whose pins do not resolve or whose type is unknown fail in <see cref="TryBuildCommand"/> (already
	/// logged) and are counted into <c>LinksUnresolved</c> rather than reaching the connect pass.
	/// </summary>
	internal static (List<ConnectCommand> Commands, int LinksUnresolved) BuildCommands(
		IReadOnlyList<TreeReshaper.Construction> constructions,
		Func<string, ITreePinHlp?> resolvePin,
		ILogger logger)
	{
		var commands = new List<ConnectCommand>();
		var linksUnresolved = 0;

		foreach (var construction in constructions)
		{
			var resolved = 0;
			var unresolved = 0;

			foreach (var link in construction.Links)
			{
				var command = TryBuildCommand(link, resolvePin, logger);
				if (command == null)
				{
					unresolved++;
					continue;
				}

				commands.Add(command.Value);
				resolved++;
			}

			linksUnresolved += unresolved;

			if (unresolved == 0)
			{
				continue;
			}

			logger.Error(
				"Node '{NodePath}': {LinkCount} links, {ResolvedCount} resolved, {UnresolvedCount} unresolved",
				construction.Path,
				construction.Links.Count,
				resolved,
				unresolved);
		}

		return (commands, linksUnresolved);
	}

	/// <summary>
	/// Resolves the link's local and external pins and packages the vendor connect call into a
	/// <see cref="ConnectCommand"/>. Returns <c>null</c> (an already-logged failure) when either pin
	/// is missing or the link type is unknown.
	/// </summary>
	private static ConnectCommand? TryBuildCommand(
		LinkEntry link,
		Func<string, ITreePinHlp?> resolvePin,
		ILogger logger)
	{
		var localPin = resolvePin(link.LocalPinPath);
		if (localPin == null)
		{
			logger.Error(
				"Link not issued, local pin missing: {LocalPin} <-> {ExternalPin} ({LinkType})",
				link.LocalPinPath,
				link.ExternalPinPath,
				link.LinkType);
			return null;
		}

		var externalPin = resolvePin(link.ExternalPinPath);
		if (externalPin == null)
		{
			logger.Error(
				"Link not issued, external pin missing: {LocalPin} <-> {ExternalPin} ({LinkType}); "
				+ "the snapshot may be stale or the consumer renamed",
				link.LocalPinPath,
				link.ExternalPinPath,
				link.LinkType);
			return null;
		}

		var connect = BuildConnectAction(link, localPin, externalPin);
		if (connect == null)
		{
			logger.Error(
				"Link not issued, unknown link type '{LinkType}': {LocalPin} <-> {ExternalPin}",
				link.LinkType,
				link.LocalPinPath,
				link.ExternalPinPath);
			return null;
		}

		return new ConnectCommand(
			LinkType: link.LinkType,
			LocalPinPath: link.LocalPinPath,
			ExternalPinPath: link.ExternalPinPath,
			Connect: connect);
	}

	/// <summary>
	/// Builds the forward vendor connect call for the link type, or <c>null</c> for an unknown
	/// type. Direct wires keep the no-arg <c>Connect</c> overload on purpose — see
	/// Docs/known_issues/05-opc-command-pin-connect-overload.md. The iconnect wire forwards the
	/// object with the plain <see cref="EConnectionType"/> (<c>ctIConnect = 2</c>) — never the
	/// read-back mask value <c>EConnectionTypeMask.ctIConnect = 4</c>; do not conflate the two enums.
	/// </summary>
	private static Action? BuildConnectAction(LinkEntry link, ITreePinHlp localPin, ITreePinHlp externalPin)
	{
		return link.LinkType switch
		{
			LinkTypes.IConnect => () => localPin.Connect(externalPin, EConnectionType.ctIConnect),
			LinkTypes.DirectPin => () => localPin.Connect(externalPin),
			LinkTypes.DirectPout => () => externalPin.Connect(localPin),
			_ => null,
		};
	}
}
