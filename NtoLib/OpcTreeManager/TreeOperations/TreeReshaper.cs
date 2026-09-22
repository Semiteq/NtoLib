using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.OpcTreeManager.Entities;

using OpcUaClient.Client.Common.Data;

using Serilog;
using Serilog.Events;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// Rebuilds a container's <c>Items</c> to match a desired spec, reaching COM only through the
/// <see cref="ISubtreeDisconnector"/> seam. It mutates the container as it walks; disconnect and swap
/// stay interleaved as the runtime requires, do not reorder them.
/// </summary>
internal static class TreeReshaper
{
	/// <summary>
	/// Reshapes <paramref name="container"/> at every nesting level and returns the constructions to
	/// reconnect plus the removal tally. <paramref name="onTreeMutationStarting"/> fires at each first
	/// write, the first subtree <c>Disconnect</c> and every container swap, never on a read.
	/// </summary>
	public static ReshapeResult Reshape(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		Func<string, (OpcScadaItemDto? Dto, IReadOnlyList<LinkEntry> Links)> resolveChild,
		ISubtreeDisconnector disconnector,
		Action onTreeMutationStarting,
		ILogger logger)
	{
		var result = new ReshapeResult();

		ApplyDesiredSpec(
			container,
			desired,
			containerPath,
			resolveChild,
			result,
			disconnector,
			onTreeMutationStarting,
			logger,
			topLevel: true);

		return result;
	}

	internal readonly record struct Construction(string Path, IReadOnlyList<LinkEntry> Links);

	/// <summary>
	/// One nesting level: constructs missing items from <paramref name="resolveChild"/>'s snapshot DTO
	/// pruned to the spec, preserves name matches, disconnects and drops the rest. Recurses into a
	/// preserved item whose spec has non-null <c>Children</c>, carrying its DTO down.
	/// </summary>
	private static void ApplyDesiredSpec(
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		Func<string, (OpcScadaItemDto? Dto, IReadOnlyList<LinkEntry> Links)> resolveChild,
		ReshapeResult result,
		ISubtreeDisconnector disconnector,
		Action onTreeMutationStarting,
		ILogger logger,
		bool topLevel)
	{
		var currentByName = container.Items.ToDictionary(i => i.Name, i => i, StringComparer.Ordinal);
		var desiredNames = new HashSet<string>(desired.Select(s => s.Name), StringComparer.Ordinal);

		// Counted per invocation, not off result: ReshapeResult accumulates across the recursive walk,
		// so its fields hold a running total and cannot feed a per-container line.
		var removedCount = 0;
		var skippedCount = 0;

		foreach (var name in currentByName.Keys.Where(n => !desiredNames.Contains(n)).ToList())
		{
			var removedPath = containerPath + "." + name;
			var (issued, threw, nodesMissing) = disconnector.DisconnectSubtree(removedPath, onTreeMutationStarting);
			removedCount++;
			result.RemovedCount++;
			result.DisconnectsIssued += issued;
			result.DisconnectsThrew += threw;
			result.NodesMissing += nodesMissing;

			if (nodesMissing == 0)
			{
				logger.Information(
					"Removed '{NodePath}': {IssuedCount} disconnects issued, {ThrewCount} threw",
					removedPath, issued, threw);
			}
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

				logger.Debug("Preserved '{NodePath}'", childPath);

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
						onTreeMutationStarting,
						logger,
						topLevel: false);
				}

				continue;
			}

			if (childDto == null)
			{
				skippedCount++;
				result.NodesNotRestored++;

				// Only a top-level name is checked against the snapshot when the plan is built, so
				// only there does an earlier Error already own this node.
				logger.Write(
					topLevel ? LogEventLevel.Debug : LogEventLevel.Error,
					"Not restored '{NodePath}': not in the container, not in the snapshot",
					childPath);
				continue;
			}

			var constructed = childDto.ToScadaItemPruned(spec);
			newItems.Add(constructed);
			constructedCount++;

			var keptPaths = EnumerateSubtreeNodePaths(constructed, childPath).ToArray();
			var filteredLinks = LinkCollector.FilterForSubtree(childLinks, keptPaths);
			result.Constructions.Add(new Construction(childPath, filteredLinks));

			if (filteredLinks.Count == 0)
			{
				logger.Error(
					"Constructed '{NodePath}': no links to connect; the snapshot holds none under the "
					+ "kept nodes, so the node comes back unwired",
					childPath);
				continue;
			}

			logger.Information(
				"Constructed '{NodePath}': {LinkCount} links to connect",
				childPath, filteredLinks.Count);
		}

		if (ItemsReferenceEqual(container.Items, newItems))
		{
			return;
		}

		onTreeMutationStarting();
		SwapContainerItems(container, newItems);

		logger.Information(
			"Reshaped '{ContainerPath}': removed={RemovedCount} constructed={ConstructedCount} "
			+ "preserved={PreservedCount} skipped={SkippedCount}",
			containerPath, removedCount, constructedCount, preservedCount, skippedCount);
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
/// constructions to reconnect plus the removal tally. A node absent from the project counts into
/// <see cref="NodesMissing"/>, never into <see cref="DisconnectsThrew"/>.
/// </summary>
internal sealed class ReshapeResult
{
	public int RemovedCount { get; set; }
	public int DisconnectsIssued { get; set; }
	public int DisconnectsThrew { get; set; }
	public int NodesMissing { get; set; }

	public List<TreeReshaper.Construction> Constructions { get; } = new();

	/// <summary>Desired nodes the walk could not construct, at any depth.</summary>
	public int NodesNotRestored { get; set; }
}
