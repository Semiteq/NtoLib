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
	private const string DollarSuffix = "$";

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

		return DedupByWire(rawLinks, log);
	}

	/// <summary>
	/// Collapses rows that describe the same physical wire. A <c>PinPout</c> pair
	/// exposes two sibling <c>ITreePinHlp</c> objects — one with a trailing <c>$</c>
	/// in <see cref="PinView.FullName"/>, one without — and an iconnect wire
	/// surfaces on both (non-$ under <c>ctIConnect</c>, $ under <c>ctGenericPin</c>).
	/// Keying by <c>(stripTrailingDollar(local), external)</c> folds the pair;
	/// when both halves are present the non-$ row wins because its linkType is
	/// semantically richer. Rows with no twin — including $-only Command wires —
	/// survive untouched and are replayed by <see cref="PlanExecutor"/>.
	/// </summary>
	private static List<LinkEntry> DedupByWire(List<LinkEntry> rows, ILogger? log)
	{
		var result = new List<LinkEntry>(rows.Count);
		var indexByKey = new Dictionary<(string StrippedLocal, string External), int>();
		var dropped = 0;

		foreach (var row in rows)
		{
			var key = (StripTrailingDollar(row.LocalPinPath), row.ExternalPinPath);

			if (!indexByKey.TryGetValue(key, out var existingIndex))
			{
				indexByKey[key] = result.Count;
				result.Add(row);
				continue;
			}

			if (IsDollarPath(result[existingIndex].LocalPinPath) && !IsDollarPath(row.LocalPinPath))
			{
				result[existingIndex] = row;
			}

			dropped++;
		}

		if (dropped > 0)
		{
			log?.Debug("Wire dedup: {DroppedCount} rows collapsed", dropped);
		}

		return result;
	}

	private static bool IsDollarPath(string path)
	{
		return path.EndsWith(DollarSuffix, StringComparison.Ordinal);
	}

	private static string StripTrailingDollar(string path)
	{
		return IsDollarPath(path) ? path.Substring(0, path.Length - 1) : path;
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
