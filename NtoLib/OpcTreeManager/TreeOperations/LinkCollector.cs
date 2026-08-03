using System;
using System.Collections.Generic;
using System.Linq;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.OpcTreeManager.Entities;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

public static class LinkCollector
{
	public static IReadOnlyList<LinkEntry> CollectAllLinks(ITreeItemHlp node, ILogger? logger = null)
	{
		if (node == null)
		{
			throw new ArgumentNullException(nameof(node));
		}

		var log = logger?.ForContext(typeof(LinkCollector));
		var pinViews = node
			.EnumAllChilds(TreeMasks.AllPinKinds, 0)
			.OfType<ITreePinHlp>()
			.Select(CreatePinView);

		var links = BuildLinks(pinViews, log);

		log?.Debug("Collected {LinkCount} links from node '{NodeFullName}'", links.Count, node.FullName);

		return links;
	}

	/// <summary>
	/// Keeps only the <see cref="LinkEntry"/>s whose <see cref="LinkEntry.LocalPinPath"/>
	/// sits under any of the given node paths. Used when replaying a pruned subtree
	/// construction: the snapshot stores the full top-level subtree's links, but
	/// for a prune-selected descendant set only the matching ones must be reconnected.
	/// A link belongs to a node when its local path starts with <c>nodePath + "."</c>.
	/// </summary>
	public static IReadOnlyList<LinkEntry> FilterForSubtree(
		IReadOnlyList<LinkEntry> links,
		IReadOnlyCollection<string> keptNodePaths)
	{
		if (links == null)
		{
			throw new ArgumentNullException(nameof(links));
		}

		if (keptNodePaths == null || keptNodePaths.Count == 0)
		{
			return Array.Empty<LinkEntry>();
		}

		var prefixes = keptNodePaths.Select(p => p + ".").ToArray();
		var result = new List<LinkEntry>();

		foreach (var link in links)
		{
			foreach (var prefix in prefixes)
			{
				if (link.LocalPinPath.StartsWith(prefix, StringComparison.Ordinal))
				{
					result.Add(link);
					break;
				}
			}
		}

		return result;
	}

	internal static IReadOnlyList<LinkEntry> BuildLinks(IEnumerable<PinView> pins, ILogger? log)
	{
		if (pins == null)
		{
			throw new ArgumentNullException(nameof(pins));
		}

		var rawLinks = new List<LinkEntry>();

		foreach (var pin in pins)
		{
			AppendLinks(pin, EConnectionTypeMask.ctGenericPin, LinkTypes.DirectPin, rawLinks, log);
			AppendLinks(pin, EConnectionTypeMask.ctGenericPout, LinkTypes.DirectPout, rawLinks, log);
			AppendLinks(pin, EConnectionTypeMask.ctIConnect, LinkTypes.IConnect, rawLinks, log);
		}

		var links = DedupByWire(rawLinks, log);
		WarnTwinlessIConnects(links, log);
		return links;
	}

	/// <summary>
	/// Capture check for the OPC PinPout <c>$</c>-sibling model (see
	/// <c>Docs/architecture/masterscada-fb-primer.md</c>): an OPC value pin surfaces as a base pin
	/// (carries the <c>iconnect</c> feedback) and a <c>$</c> sibling (carries the <c>directPin</c>
	/// input). For each captured <c>iconnect</c> this classifies the input sibling by structure and
	/// only alarms on the one shape that has ever meant lost data:
	/// <list type="bullet">
	/// <item>sibling captured to the <b>same</b> external — both halves present; silent.</item>
	/// <item>sibling captured to a <b>different</b> external — the expected shape for a pin whose input
	/// and feedback target different nodes (feedback-only pins); logged at Debug.</item>
	/// <item>sibling has <b>no captured row at all</b> — the only shape a dropped or blind/partial
	/// capture takes (the old <see cref="DedupByWire"/> fold, or the read-back blindness of
	/// known-issue 11); logged at Warning.</item>
	/// </list>
	/// Valid only where the PinPout model holds (OPC subtrees). If <see cref="LinkCollector"/> is ever
	/// run over non-OPC object trees (FB-to-FB iconnects have no <c>$</c> siblings), the Warning branch
	/// would misfire.
	/// </summary>
	private static void WarnTwinlessIConnects(IReadOnlyList<LinkEntry> links, ILogger? log)
	{
		if (log == null)
		{
			return;
		}

		var directPinExternalsByLocal = links
			.Where(row => row.LinkType == LinkTypes.DirectPin)
			.ToLookup(row => row.LocalPinPath, row => row.ExternalPinPath);

		foreach (var row in links)
		{
			if (row.LinkType != LinkTypes.IConnect)
			{
				continue;
			}

			var sibling = row.LocalPinPath + "$";
			var siblingExternals = directPinExternalsByLocal[sibling];

			if (siblingExternals.Contains(row.ExternalPinPath))
			{
				// Both halves captured to the same external — settings pin, nothing to flag.
				continue;
			}

			if (siblingExternals.Any())
			{
				log.Debug(
					"Feedback-only iconnect '{LocalPin}' → '{ExternalPin}': input sibling '{Sibling}' is " +
					"captured to a different external — expected PinPout shape",
					row.LocalPinPath, row.ExternalPinPath, sibling);
				continue;
			}

			log.Warning(
				"Capture check: iconnect '{LocalPin}' → '{ExternalPin}' has no captured link on input " +
				"sibling '{Sibling}' — the input is either genuinely unconnected or was not captured " +
				"(partial/blind capture; see Docs/known_issues/11)",
				row.LocalPinPath, row.ExternalPinPath, sibling);
		}
	}

	/// <summary>
	/// Removes only exact duplicate rows — same <see cref="LinkEntry.LocalPinPath"/>,
	/// <see cref="LinkEntry.ExternalPinPath"/> AND <see cref="LinkEntry.LinkType"/> — which arise
	/// when a pin matches more than one mask and the same wire is enumerated twice.
	/// <para>
	/// It does NOT fold the two halves of a <c>PinPout</c> pair. A pin and its <c>$</c> sibling carry
	/// <b>distinct</b> wires: on <c>CHx.Kp</c> the base pin holds an <c>iconnect</c> (feedback) and
	/// <c>CHx.Kp$</c> holds a <c>directPin</c> (input), both to the same external element. Both must
	/// survive — the <c>directPin</c> input is what reconnects reliably and makes the pin show
	/// connected after a reload; the earlier <c>$</c>-twin fold dropped it, so every restored iconnect
	/// pin came back with only its feedback half and never persisted.
	/// </para>
	/// </summary>
	private static List<LinkEntry> DedupByWire(List<LinkEntry> rows, ILogger? log)
	{
		var result = new List<LinkEntry>(rows.Count);
		var seen = new HashSet<(string Local, string External, string LinkType)>();
		var dropped = 0;

		foreach (var row in rows)
		{
			var key = (row.LocalPinPath, row.ExternalPinPath, row.LinkType);

			if (!seen.Add(key))
			{
				dropped++;
				log?.Warning(
					"Exact-duplicate dedup: dropped ({LocalPin}, {ExternalPin}, {LinkType}) — " +
					"an identical surviving triple already carries this wire",
					row.LocalPinPath, row.ExternalPinPath, row.LinkType);
				continue;
			}

			result.Add(row);
		}

		if (dropped > 0)
		{
			log?.Warning("Exact-duplicate dedup: {DroppedCount} rows removed", dropped);
		}

		return result;
	}

	private static void AppendLinks(
		PinView pin,
		EConnectionTypeMask mask,
		string linkType,
		List<LinkEntry> target,
		ILogger? log)
	{
		foreach (var externalFullName in pin.GetConnections(mask))
		{
			target.Add(new LinkEntry
			{
				LocalPinPath = pin.FullName,
				ExternalPinPath = externalFullName,
				LinkType = linkType,
			});

			log?.Debug(
				"CollectLink {LinkType} {LocalPin} ↔ {ExternalPin}",
				linkType, pin.FullName, externalFullName);
		}
	}

	private static PinView CreatePinView(ITreePinHlp pin)
	{
		return new PinView(
			pin.Name,
			pin.FullName,
			mask => pin.GetConnections(mask).Select(peer => peer.FullName));
	}
}

internal readonly record struct PinView(
	string Name,
	string FullName,
	Func<EConnectionTypeMask, IEnumerable<string>> GetConnections);
