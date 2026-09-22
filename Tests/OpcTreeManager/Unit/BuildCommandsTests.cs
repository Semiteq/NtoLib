using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class BuildCommandsTests
{
	[Fact]
	public void BuildCommands_ExternalPinMissing_LineNamesBothEnds()
	{
		var sink = new CapturingSink();
		var pins = new Dictionary<string, ITreePinHlp> { ["Opc.CBr4.Kp"] = Pin() };

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			Constructions(Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", LinkTypes.IConnect)),
			path => pins.TryGetValue(path, out var pin) ? pin : null,
			sink.ToLogger());

		commands.Should().BeEmpty();
		unresolved.Should().Be(1);

		var error = SingleLinkError(sink);
		error.MessageTemplate.Text.Should().Be(
			"Link not issued, external pin missing: {LocalPin} <-> {ExternalPin} ({LinkType}); "
			+ "the snapshot may be stale or the consumer renamed");
		CapturingSink.Property(error, "LocalPin").Should().Be("Opc.CBr4.Kp");
		CapturingSink.Property(error, "ExternalPin").Should().Be("Cabinet.CBr4.Kp");
		CapturingSink.Property(error, "LinkType").Should().Be(LinkTypes.IConnect);
	}

	[Fact]
	public void BuildCommands_LocalPinMissing_LineNamesBothEnds()
	{
		var sink = new CapturingSink();
		var pins = new Dictionary<string, ITreePinHlp> { ["Cabinet.CBr4.Kp"] = Pin() };

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			Constructions(Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", LinkTypes.DirectPin)),
			path => pins.TryGetValue(path, out var pin) ? pin : null,
			sink.ToLogger());

		commands.Should().BeEmpty();
		unresolved.Should().Be(1);

		var error = SingleLinkError(sink);
		error.MessageTemplate.Text.Should().Be(
			"Link not issued, local pin missing: {LocalPin} <-> {ExternalPin} ({LinkType})");
		CapturingSink.Property(error, "LocalPin").Should().Be("Opc.CBr4.Kp");
		CapturingSink.Property(error, "ExternalPin").Should().Be("Cabinet.CBr4.Kp");
		CapturingSink.Property(error, "LinkType").Should().Be(LinkTypes.DirectPin);
	}

	[Fact]
	public void BuildCommands_UnknownLinkType_LineNamesTypeAndBothEnds()
	{
		var sink = new CapturingSink();
		var pins = new Dictionary<string, ITreePinHlp>
		{
			["Opc.CBr4.Kp"] = Pin(),
			["Cabinet.CBr4.Kp"] = Pin(),
		};

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			Constructions(Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", "sideways")),
			path => pins.TryGetValue(path, out var pin) ? pin : null,
			sink.ToLogger());

		commands.Should().BeEmpty();
		unresolved.Should().Be(1);

		var error = SingleLinkError(sink);
		error.MessageTemplate.Text.Should().Be(
			"Link not issued, unknown link type '{LinkType}': {LocalPin} <-> {ExternalPin}");
		CapturingSink.Property(error, "LinkType").Should().Be("sideways");
		CapturingSink.Property(error, "LocalPin").Should().Be("Opc.CBr4.Kp");
		CapturingSink.Property(error, "ExternalPin").Should().Be("Cabinet.CBr4.Kp");
	}

	[Fact]
	public void BuildCommands_BothPinsResolve_YieldsACommandAndLogsNoError()
	{
		var sink = new CapturingSink();
		var pins = new Dictionary<string, ITreePinHlp>
		{
			["Opc.CBr4.Kp"] = Pin(),
			["Cabinet.CBr4.Kp"] = Pin(),
		};

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			Constructions(Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", LinkTypes.IConnect)),
			path => pins.TryGetValue(path, out var pin) ? pin : null,
			sink.ToLogger());

		unresolved.Should().Be(0);
		commands.Should().ContainSingle();
		commands[0].LocalPinPath.Should().Be("Opc.CBr4.Kp");
		commands[0].ExternalPinPath.Should().Be("Cabinet.CBr4.Kp");
		sink.Events.Should().NotContain(e => e.Level == LogEventLevel.Error);
		NodeTallies(sink).Should().BeEmpty("a node whose links all resolve has nothing to report");
	}

	[Fact]
	public void BuildCommands_NodeWithNoResolvableLink_LogsTheNodeTally()
	{
		var sink = new CapturingSink();

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			Constructions(
				Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", LinkTypes.IConnect),
				Link("Opc.CBr4.Ki", "Cabinet.CBr4.Ki", LinkTypes.DirectPin)),
			_ => null,
			sink.ToLogger());

		commands.Should().BeEmpty();
		unresolved.Should().Be(2);

		var tally = NodeTallies(sink).Single();
		tally.Level.Should().Be(LogEventLevel.Error);
		tally.MessageTemplate.Text.Should().Be(
			"Node '{NodePath}': {LinkCount} links, {ResolvedCount} resolved, {UnresolvedCount} unresolved",
			"the tally is written before any connect is attempted, so both halves are build-time words");
		CapturingSink.Property(tally, "NodePath").Should().Be("Opc.CBr4");
		CapturingSink.Property(tally, "LinkCount").Should().Be("2");
		CapturingSink.Property(tally, "ResolvedCount").Should().Be("0");
		CapturingSink.Property(tally, "UnresolvedCount").Should().Be("2");
	}

	[Fact]
	public void BuildCommands_SeveralConstructions_TallyCountsPerConstruction()
	{
		var sink = new CapturingSink();
		var pins = new Dictionary<string, ITreePinHlp>
		{
			["Opc.CBr4.Kp"] = Pin(),
			["Cabinet.CBr4.Kp"] = Pin(),
		};

		var constructions = new[]
		{
			new TreeReshaper.Construction(
				"Opc.CBr4",
				new[]
				{
					Link("Opc.CBr4.Kp", "Cabinet.CBr4.Kp", LinkTypes.IConnect),
					Link("Opc.CBr4.Ki", "Cabinet.CBr4.Ki", LinkTypes.DirectPin),
				}),
			new TreeReshaper.Construction(
				"Opc.Water",
				new[] { Link("Opc.Water.Flow", "Cabinet.Water.Flow", LinkTypes.DirectPin) }),
		};

		var (commands, unresolved) = PlanExecutor.BuildCommands(
			constructions,
			path => pins.TryGetValue(path, out var pin) ? pin : null,
			sink.ToLogger());

		commands.Should().ContainSingle();
		unresolved.Should().Be(2);

		var tallies = NodeTallies(sink);
		tallies.Should().HaveCount(2);

		CapturingSink.Property(tallies[0], "NodePath").Should().Be("Opc.CBr4");
		CapturingSink.Property(tallies[0], "LinkCount").Should().Be("2");
		CapturingSink.Property(tallies[0], "ResolvedCount").Should().Be("1");
		CapturingSink.Property(tallies[0], "UnresolvedCount").Should().Be("1");

		CapturingSink.Property(tallies[1], "NodePath").Should().Be("Opc.Water");
		CapturingSink.Property(tallies[1], "LinkCount").Should().Be("1");
		CapturingSink.Property(tallies[1], "ResolvedCount").Should().Be("0");
		CapturingSink.Property(tallies[1], "UnresolvedCount").Should().Be("1");
	}

	private static LinkEntry Link(string localPinPath, string externalPinPath, string linkType)
	{
		return new LinkEntry
		{
			LocalPinPath = localPinPath,
			ExternalPinPath = externalPinPath,
			LinkType = linkType,
		};
	}

	private static IReadOnlyList<TreeReshaper.Construction> Constructions(params LinkEntry[] links)
	{
		return new[] { new TreeReshaper.Construction("Opc.CBr4", links) };
	}

	/// <summary>
	/// A pin stand-in: the build pass only checks it for null and captures it into a lambda it never
	/// invokes, and the wrapper's constructor accepts a null COM pin.
	/// </summary>
	private static ITreePinHlp Pin()
	{
		return new ITreePinHlp(null!);
	}

	private static LogEvent SingleLinkError(CapturingSink sink)
	{
		return sink.Events.Single(e =>
			e.Level == LogEventLevel.Error
			&& e.MessageTemplate.Text.StartsWith("Link not issued", StringComparison.Ordinal));
	}

	private static IReadOnlyList<LogEvent> NodeTallies(CapturingSink sink)
	{
		return sink.Events
			.Where(e => e.MessageTemplate.Text.StartsWith("Node '{NodePath}'", StringComparison.Ordinal))
			.ToList();
	}
}
