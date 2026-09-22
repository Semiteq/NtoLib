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

		var pinViews = node
			.EnumAllChilds(TreeMasks.AllPinKinds, 0)
			.OfType<ITreePinHlp>()
			.Select(CreatePinView);

		return CollectLinks(node.FullName, pinViews, logger);
	}

	internal static IReadOnlyList<LinkEntry> CollectLinks(
		string nodePath,
		IEnumerable<PinView> pins,
		ILogger? logger)
	{
		var links = BuildLinks(pins, logger);

		logger?.Information("Captured '{NodePath}': {LinkCount} links", nodePath, links.Count);

		return links;
	}

	/// <summary>
	/// Keeps the links whose <see cref="LinkEntry.LocalPinPath"/> starts with a kept node path and a
	/// dot. Replaying a pruned construction reconnects only the selected descendants.
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

		var links = DedupByWire(rawLinks);
		WarnTwinlessIConnects(links, log);
		return links;
	}

	/// <summary>
	/// Warns on a captured iconnect whose <c>$</c> input sibling has no captured row, the one shape
	/// that has meant lost data. Holds only for OPC subtrees, where the PinPout sibling model applies.
	/// See Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md.
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
				// Both halves captured to the same external: a settings pin, nothing to flag.
				continue;
			}

			var siblingExternal = siblingExternals.FirstOrDefault();

			if (siblingExternal != null)
			{
				log.Debug(
					"Iconnect {LocalPin} <-> {ExternalPin}: input sibling '{Sibling}' captured to " +
					"'{SiblingExternal}'",
					row.LocalPinPath, row.ExternalPinPath, sibling, siblingExternal);
				continue;
			}

			log.Warning(
				"Iconnect {LocalPin} <-> {ExternalPin} captured without an input link on '{Sibling}'",
				row.LocalPinPath, row.ExternalPinPath, sibling);
		}
	}

	/// <summary>
	/// Drops exact duplicate rows only: same local pin, external pin and link type, which a pin
	/// matching two masks produces. A pin and its <c>$</c> sibling stay separate rows, distinct wires.
	/// See Docs/known_issues/11-opc-pinpout-sibling-and-iconnect-connect.md.
	/// </summary>
	private static List<LinkEntry> DedupByWire(List<LinkEntry> rows)
	{
		var result = new List<LinkEntry>(rows.Count);
		var seen = new HashSet<(string Local, string External, string LinkType)>();

		foreach (var row in rows)
		{
			if (seen.Add((row.LocalPinPath, row.ExternalPinPath, row.LinkType)))
			{
				result.Add(row);
			}
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
				"Captured {LinkType} {LocalPin} <-> {ExternalPin}",
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
