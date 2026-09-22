using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.Facade;

using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class SnapshotSummaryTests
{
	[Fact]
	public void LogSnapshotWritten_EveryNodeHasLinks_IsInformation()
	{
		var sink = new CapturingSink();

		OpcTreeManagerService.LogSnapshotWritten(
			groupName: "MBE",
			treeJsonPath: @"C:\Config\tree.json",
			snapshot: Snapshot(("CBr4", 2), ("Water", 1)),
			logger: sink.ToLogger());

		var summary = sink.Events.Single();

		summary.Level.Should().Be(LogEventLevel.Information);
		summary.MessageTemplate.Text.Should().EndWith("{NodesWithoutLinks} without links");
		CapturingSink.Property(summary, "NodeCount").Should().Be("2");
		CapturingSink.Property(summary, "LinkCount").Should().Be("3");
		CapturingSink.Property(summary, "NodesWithoutLinks").Should().Be("0");
	}

	[Fact]
	public void LogSnapshotWritten_NodeWithoutLinks_IsErrorAndCounted()
	{
		var sink = new CapturingSink();

		OpcTreeManagerService.LogSnapshotWritten(
			groupName: "MBE",
			treeJsonPath: @"C:\Config\tree.json",
			snapshot: Snapshot(("CBr4", 2), ("Water", 0)),
			logger: sink.ToLogger());

		var summary = sink.Events.Single();

		summary.Level.Should().Be(LogEventLevel.Error);
		summary.MessageTemplate.Text.Should().EndWith(
			"; the next rebuild constructs those nodes unwired");
		CapturingSink.Property(summary, "NodesWithoutLinks").Should().Be("1");
	}

	private static Dictionary<string, NodeSnapshot> Snapshot(params (string Name, int LinkCount)[] nodes)
	{
		return nodes.ToDictionary(
			node => node.Name,
			node => new NodeSnapshot { Links = Links(node.Name, node.LinkCount) });
	}

	private static IReadOnlyList<LinkEntry> Links(string nodeName, int count)
	{
		return Enumerable.Range(0, count)
			.Select(i => new LinkEntry
			{
				LocalPinPath = $"Opc.{nodeName}.Pin{i}",
				ExternalPinPath = $"Cabinet.{nodeName}.Pin{i}",
				LinkType = LinkTypes.DirectPin,
			})
			.ToList();
	}
}
