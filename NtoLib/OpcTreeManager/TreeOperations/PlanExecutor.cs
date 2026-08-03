using System;
using System.Collections.Generic;

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

	public PlanExecutor(IProjectHlp project, ISubtreeDisconnector disconnector, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
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
	/// once at the group level, then connects every link in-pass on this single
	/// tick — direct links first, iconnect last. Runs once, after the host clears
	/// <see cref="IProjectHlp.InRuntime"/> (the wait lives in <c>DeferredExecutor</c>).
	/// Success is judged by the SCADA tree on reload, not in code: an in-code read-back via
	/// <c>GetConnections</c> is blind after the structural commit (a connected link reads back
	/// as absent), so the summary reports only what is known — connects issued and how many threw.
	/// See <c>Docs/known_issues/11</c>.
	/// </summary>
	public Result Execute(RebuildPlan plan)
	{
		if (plan == null)
		{
			throw new ArgumentNullException(nameof(plan));
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
			logger: _logger);

		ResetScadaItemsMap(protocol);
		protocol.SynchWihSysTree();

		var commitResult = CommitStructuralChange(plan.OpcFbPath);
		if (commitResult.IsFailed)
		{
			return commitResult;
		}

		var (commands, resolveFail) = BuildCommands(reshape.Constructions);

		var orderedCommands = CommandOrdering.OrderCommandsDirectFirst(commands);

		var (connectsIssued, connectsThrew) = ConnectRunner.ConnectAll(orderedCommands, _logger);

		LogExecutionComplete(
			shrinkCount: reshape.ShrinkCount,
			constructionsCount: reshape.Constructions.Count,
			shrinkTotal: reshape.ShrinkTotal,
			shrinkSuccess: reshape.ShrinkSuccess,
			shrinkFail: reshape.ShrinkFail,
			resolveFail: resolveFail,
			connectsIssued: connectsIssued,
			connectsThrew: connectsThrew);

		return Result.Ok();
	}

	/// <summary>
	/// Emits the single <c>Execution complete</c> summary. <paramref name="resolveFail"/> counts commands
	/// that failed to build (pin unresolved or unknown link type) and so were never issued.
	/// </summary>
	private void LogExecutionComplete(
		int shrinkCount,
		int constructionsCount,
		int shrinkTotal,
		int shrinkSuccess,
		int shrinkFail,
		int resolveFail,
		int connectsIssued,
		int connectsThrew)
	{
		_logger.Information(
			"Execution complete: shrink={ShrinkCount} expand={ExpandCount}; "
			+ "disconnect links total={ShrinkTotal} ok={ShrinkSuccess} fail={ShrinkFail}; "
			+ "connects issued={ConnectsIssued} threw={ConnectsThrew} unresolved={ResolveFail} "
			+ "(success judged by the SCADA tree on reload, not by read-back)",
			shrinkCount, constructionsCount, shrinkTotal, shrinkSuccess, shrinkFail,
			connectsIssued, connectsThrew, resolveFail);
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
		// Self-assignment triggers the setter's map-reset side effect (see summary); CA2245 flags it.
		var root = protocol.ScadaRootNode;
#pragma warning disable CA2245
		protocol.ScadaRootNode = root;
#pragma warning restore CA2245
	}

	/// <summary>
	/// Builds a <see cref="ConnectCommand"/> for every resolvable link across all constructions. Links
	/// whose pins do not resolve or whose type is unknown fail in <see cref="TryBuildCommand"/> (already
	/// logged) and are counted into <c>ResolveFail</c> rather than reaching the connect pass.
	/// </summary>
	private (List<ConnectCommand> Commands, int ResolveFail) BuildCommands(IReadOnlyList<TreeReshaper.Construction> constructions)
	{
		var commands = new List<ConnectCommand>();
		var resolveFail = 0;

		foreach (var construction in constructions)
		{
			foreach (var link in construction.Links)
			{
				var command = TryBuildCommand(link);
				if (command == null)
				{
					resolveFail++;
					continue;
				}

				commands.Add(command.Value);
			}
		}

		return (commands, resolveFail);
	}

	/// <summary>
	/// Resolves the link's local and external pins and packages the vendor connect call into a
	/// <see cref="ConnectCommand"/>. Returns <c>null</c> (an already-logged failure) when either pin
	/// is missing or the link type is unknown.
	/// </summary>
	private ConnectCommand? TryBuildCommand(LinkEntry link)
	{
		var localPin = _project.SafeItem<ITreePinHlp>(link.LocalPinPath);
		if (localPin == null)
		{
			_logger.Error("Connect — local pin not found: {Path}", link.LocalPinPath);
			return null;
		}

		var externalPin = _project.SafeItem<ITreePinHlp>(link.ExternalPinPath);
		if (externalPin == null)
		{
			_logger.Error("Connect — external pin not found: {Path}", link.ExternalPinPath);
			return null;
		}

		var connect = BuildConnectAction(link, localPin, externalPin);
		if (connect == null)
		{
			_logger.Error(
				"Connect {LocalPin} ↔ {ExternalPin} — unknown link type '{LinkType}'",
				link.LocalPinPath,
				link.ExternalPinPath,
				link.LinkType);
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
