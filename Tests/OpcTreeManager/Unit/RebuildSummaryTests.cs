using System;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class RebuildSummaryTests
{
	[Fact]
	public void LogRebuildFinished_NothingFailed_IsInformationAndSaysSo()
	{
		var sink = new CapturingSink();
		var reshape = new ReshapeResult { RemovedCount = 2, DisconnectsIssued = 9 };

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: reshape,
			connectsIssued: 14,
			connectsThrew: 0,
			linksUnresolved: 0,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Information);
		summary.MessageTemplate.Text.Should().StartWith(
			"Rebuild of group '{GroupName}' finished without failures:");
		summary.MessageTemplate.Text.Should().EndWith("unresolved={LinksUnresolved}");
		CapturingSink.Property(summary, "GroupName").Should().Be("MBE");
		CapturingSink.Property(summary, "RemovedCount").Should().Be("2");
		CapturingSink.Property(summary, "DisconnectsIssued").Should().Be("9");
		CapturingSink.Property(summary, "ConnectsIssued").Should().Be("14");
	}

	[Fact]
	public void LogRebuildFinished_LinksUnresolved_IsErrorAndSaysTheRunDidNotSucceed()
	{
		var sink = new CapturingSink();

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: new ReshapeResult(),
			connectsIssued: 0,
			connectsThrew: 0,
			linksUnresolved: 119,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Error);
		summary.MessageTemplate.Text.Should().StartWith(
			"Rebuild of group '{GroupName}' finished with failures:");
		summary.MessageTemplate.Text.Should().EndWith(
			"; the nodes are in the tree with links missing, close the project without saving, "
			+ "then re-capture the snapshot or repair the consumers");
		CapturingSink.Property(summary, "LinksUnresolved").Should().Be("119");
	}

	[Fact]
	public void LogRebuildFinished_NodeMissingAndLinksUnresolved_TellsTheStructuralAction()
	{
		var sink = new CapturingSink();

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: new ReshapeResult { RemovedCount = 1, NodesMissing = 1 },
			connectsIssued: 0,
			connectsThrew: 0,
			linksUnresolved: 119,
			logger: sink.ToLogger());

		Summary(sink).MessageTemplate.Text.Should().EndWith(
			"; the tree is partially rebuilt, close the project without saving");
	}

	[Fact]
	public void LogRebuildFinished_NodeMissingFromProject_IsError()
	{
		var sink = new CapturingSink();

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: new ReshapeResult { RemovedCount = 1, NodesMissing = 1 },
			connectsIssued: 0,
			connectsThrew: 0,
			linksUnresolved: 0,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Error);
		CapturingSink.Property(summary, "NodesMissing").Should().Be("1");
	}

	[Fact]
	public void LogRebuildFinished_NodeNotRestored_IsError()
	{
		var sink = new CapturingSink();

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: new ReshapeResult { NodesNotRestored = 1 },
			connectsIssued: 0,
			connectsThrew: 0,
			linksUnresolved: 0,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Error);
		summary.MessageTemplate.Text.Should().StartWith(
			"Rebuild of group '{GroupName}' finished with failures:");
		CapturingSink.Property(summary, "NodesNotRestored").Should().Be("1");
	}

	[Fact]
	public void LogRebuildFinished_ConstructionWithoutLinks_IsError()
	{
		var sink = new CapturingSink();
		var reshape = new ReshapeResult();
		reshape.Constructions.Add(new TreeReshaper.Construction("Root.Group.A", Array.Empty<LinkEntry>()));
		reshape.Constructions.Add(new TreeReshaper.Construction("Root.Group.B", Array.Empty<LinkEntry>()));

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: reshape,
			connectsIssued: 0,
			connectsThrew: 0,
			linksUnresolved: 0,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Error);
		summary.MessageTemplate.Text.Should().StartWith(
			"Rebuild of group '{GroupName}' finished with failures:");
		summary.MessageTemplate.Text.Should().EndWith("repair the consumers");
		CapturingSink.Property(summary, "ConstructionsWithoutLinks").Should().Be("2");
	}

	[Fact]
	public void LogRebuildFinished_ConnectThrew_IsError()
	{
		var sink = new CapturingSink();

		PlanExecutor.LogRebuildFinished(
			groupName: "MBE",
			reshape: new ReshapeResult(),
			connectsIssued: 3,
			connectsThrew: 1,
			linksUnresolved: 0,
			logger: sink.ToLogger());

		var summary = Summary(sink);

		summary.Level.Should().Be(LogEventLevel.Error);
		summary.MessageTemplate.Text.Should().EndWith(
			"; the tree is partially rebuilt, close the project without saving");
	}

	private static LogEvent Summary(CapturingSink sink)
	{
		return sink.Events.Single();
	}
}
