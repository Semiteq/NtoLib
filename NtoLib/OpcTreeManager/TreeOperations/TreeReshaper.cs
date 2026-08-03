using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.OpcTreeManager.Entities;

using OpcUaClient.Client.Common.Data;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// COM-free tree-reshaping core. Rebuilds a container's <c>Items</c> to match a desired spec by
/// disconnecting removed subtrees (through the <see cref="ISubtreeDisconnector"/> seam), constructing
/// missing ones pruned to the spec, and preserving matches. Drives live disconnects and mutates the
/// container during the walk — COM-free by delegation, not side-effect-free: disconnect and swap stay
/// interleaved exactly as the runtime requires; do not reorder them.
/// </summary>
internal static class TreeReshaper
{
	/// <summary>
	/// Rebuilds <paramref name="container"/>'s <c>Items</c> to match <paramref name="desired"/> at every
	/// nesting level, disconnecting removed subtrees and constructing missing ones pruned to the spec.
	/// Returns the constructions to reconnect plus the disconnect (shrink) tally.
	/// </summary>
	public static ReshapeResult Reshape(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		Func<string, (OpcScadaItemDto? Dto, IReadOnlyList<LinkEntry> Links)> resolveChild,
		ISubtreeDisconnector disconnector,
		ILogger logger)
	{
		var result = new ReshapeResult();
		ApplyDesiredSpec(container, desired, containerPath, resolveChild, result, disconnector, logger);
		return result;
	}

	internal readonly record struct Construction(string Path, IReadOnlyList<LinkEntry> Links);

	/// <summary>
	/// Rebuilds <paramref name="container"/>'s <c>Items</c> to match <paramref name="desired"/>.
	/// Missing items are constructed from the snapshot DTO returned by
	/// <paramref name="resolveChild"/> (pruned to match the spec's children),
	/// existing items whose names match are preserved. Removed items are live-disconnected
	/// and then dropped. Recurses into each preserved item whose spec has non-null
	/// <c>Children</c>, carrying the resolved DTO downwards so deep constructions can
	/// walk the same snapshot subtree.
	/// </summary>
	private static void ApplyDesiredSpec(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		Func<string, (OpcScadaItemDto? Dto, IReadOnlyList<LinkEntry> Links)> resolveChild,
		ReshapeResult result,
		ISubtreeDisconnector disconnector,
		ILogger logger)
	{
		var currentByName = container.Items.ToDictionary(i => i.Name, i => i, StringComparer.Ordinal);
		var desiredNames = new HashSet<string>(desired.Select(s => s.Name), StringComparer.Ordinal);

		foreach (var name in currentByName.Keys.Where(n => !desiredNames.Contains(n)).ToList())
		{
			var (total, success, fail) = disconnector.DisconnectSubtree(containerPath + "." + name);
			result.ShrinkCount++;
			result.ShrinkTotal += total;
			result.ShrinkSuccess += success;
			result.ShrinkFail += fail;
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

				logger.Debug("BuildNewItems — preserved '{NodePath}' (links intact, no reconnect)", childPath);

				if (spec.Children != null)
				{
					ApplyDesiredSpec(
						existing,
						spec.Children,
						childPath,
						inner => childDto != null
							? (childDto.Items.FirstOrDefault(i => i.Name == inner), childLinks)
							: (null, Array.Empty<LinkEntry>()),
						result,
						disconnector,
						logger);
				}

				continue;
			}

			if (childDto == null)
			{
				logger.Warning(
					"BuildNewItems — node '{NodePath}' not in current container and not in snapshot; skipped.",
					childPath);
				continue;
			}

			var constructed = childDto.ToScadaItemPruned(spec);
			newItems.Add(constructed);
			constructedCount++;

			var keptPaths = EnumerateSubtreeNodePaths(constructed, childPath).ToArray();
			var filteredLinks = LinkCollector.FilterForSubtree(childLinks, keptPaths);
			result.Constructions.Add(new Construction(childPath, filteredLinks));

			logger.Debug(
				"BuildNewItems — newly constructed '{NodePath}' ({LinkCount} links to reconnect)",
				childPath, filteredLinks.Count);
		}

		logger.Information(
			"BuildNewItems at '{ContainerPath}' — desired={DesiredCount} preserved={PreservedCount} newlyConstructed={NewlyConstructedCount}",
			containerPath, desired.Count, preservedCount, constructedCount);

		if (ItemsReferenceEqual(container.Items, newItems))
		{
			return;
		}

		SwapContainerItems(container, newItems);
	}

	private static bool ItemsReferenceEqual(IList<OpcUaScadaItem> current, List<OpcUaScadaItem> candidate)
	{
		if (current.Count != candidate.Count)
		{
			return false;
		}

		for (var i = 0; i < candidate.Count; i++)
		{
			if (!ReferenceEquals(current[i], candidate[i]))
			{
				return false;
			}
		}

		return true;
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
}

/// <summary>
/// Mutable accumulator for a <see cref="TreeReshaper.Reshape"/> pass, returned as its result: the
/// constructions to reconnect plus the disconnect (shrink) tally. The four shrink numbers are each
/// consumed by the execution summary — do not reduce them to one count.
/// </summary>
internal sealed class ReshapeResult
{
	public List<TreeReshaper.Construction> Constructions { get; } = new();
	public int ShrinkCount { get; set; }
	public int ShrinkTotal { get; set; }
	public int ShrinkSuccess { get; set; }
	public int ShrinkFail { get; set; }
}
