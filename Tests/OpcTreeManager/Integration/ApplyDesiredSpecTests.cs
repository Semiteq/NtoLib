using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.Facade;
using NtoLib.OpcTreeManager.TreeOperations;

using OpcUaClient.Client.Common.Data;

using Serilog.Events;

using Tests.OpcTreeManager.Integration.Fakes;

using Xunit;

namespace Tests.OpcTreeManager.Integration;

/// <summary>
/// Seam-based tests for <see cref="TreeReshaper.Reshape"/>.
/// All vendor COM calls are replaced by <see cref="FakeSubtreeDisconnector"/>.
/// </summary>
public sealed class ApplyDesiredSpecTests
{
	// Helpers

	private static OpcUaScadaItem ScadaItem(string name, params OpcUaScadaItem[] children)
	{
		var item = new OpcUaScadaItem { Name = name };
		foreach (var child in children)
		{
			item.Items.Add(child);
		}

		return item;
	}

	private static OpcScadaItemDto DtoNode(string name, params OpcScadaItemDto[] children)
	{
		return new OpcScadaItemDto
		{
			Name = name,
			PinValueType = "0",
			DeadbandType = "None",
			Items = children.ToList(),
		};
	}

	private static NodeSnapshot Snapshot(OpcScadaItemDto dto, params LinkEntry[] links)
	{
		return new NodeSnapshot
		{
			ScadaItem = dto,
			Links = links,
		};
	}

	private static LinkEntry Link(string localPin, string externalPin)
	{
		return new LinkEntry
		{
			LocalPinPath = localPin,
			ExternalPinPath = externalPin,
			LinkType = LinkTypes.DirectPin,
		};
	}

	private static NodeSpec Leaf(string name)
	{
		return new NodeSpec(name, null);
	}

	private static NodeSpec Branch(string name, params NodeSpec[] children)
	{
		return new NodeSpec(name, children);
	}

	private static FakeSubtreeDisconnector MakeDisconnector()
	{
		return new FakeSubtreeDisconnector();
	}

	private static void Invoke(
		FakeSubtreeDisconnector disconnector,
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		out List<TreeReshaper.Construction> constructions,
		out int shrinkCount)
	{
		var result = TreeReshaper.Reshape(
			container,
			desired,
			containerPath,
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			disconnector,
			() => { },
			Serilog.Core.Logger.None);
		constructions = result.Constructions;
		shrinkCount = result.RemovedCount;
	}

	private static CapturingSink ReshapeCapturingLog(
		FakeSubtreeDisconnector disconnector,
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot)
	{
		var sink = new CapturingSink();

		TreeReshaper.Reshape(
			container,
			desired,
			containerPath,
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			disconnector,
			() => { },
			sink.ToLogger());

		return sink;
	}

	private static List<LogEvent> EventsOf(CapturingSink sink, string messageTemplate)
	{
		return sink.Events.Where(e => e.MessageTemplate.Text == messageTemplate).ToList();
	}

	// Case 1: single-level shrink

	[Fact]
	public void SingleLevelShrink_RemovesExcessNode_AndRecordsDisconnect()
	{
		var container = ScadaItem("Group",
			ScadaItem("A"),
			ScadaItem("B"),
			ScadaItem("C"));

		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["B"] = Snapshot(DtoNode("B")),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Select(i => i.Name).Should().Equal("A", "B");
		shrinkCount.Should().Be(1);
		disconnector.RecordedPaths.Should().ContainSingle(p => p.EndsWith(".C"));
		constructions.Should().BeEmpty();
	}

	// Case 2: single-level expand, new node present in snapshot

	[Fact]
	public void SingleLevelExpand_AddsNewNodeFromSnapshot()
	{
		var container = ScadaItem("Group", ScadaItem("A"));

		var desired = new[] { Leaf("A"), Leaf("B") };
		var bLinks = new[]
		{
			Link("Root.Group.B.Pin1", "Consumers.FB.Input"),
		};
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["B"] = Snapshot(DtoNode("B"), bLinks[0]),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Select(i => i.Name).Should().Equal("A", "B");
		shrinkCount.Should().Be(0);
		disconnector.RecordedPaths.Should().BeEmpty();
		constructions.Should().ContainSingle();
		constructions[0].Path.Should().EndWith(".B");
		constructions[0].Links.Should().HaveCount(1);
	}

	// Case 3: nested expand, Valves preserved, one new child added

	[Fact]
	public void NestedExpand_PreservesExistingChild_ConstructsMissingChild()
	{
		var container = ScadaItem("Group",
			ScadaItem("Valves", ScadaItem("VPG1")));

		var desired = new[]
		{
			Branch("Valves", Leaf("VPG1"), Leaf("VPG2")),
		};

		// Snapshot Valves DTO has VPG1, VPG2, VPG4
		var valvesDto = DtoNode("Valves",
			DtoNode("VPG1"),
			DtoNode("VPG2"),
			DtoNode("VPG4"));

		var vpg2Link = Link("Root.Group.Valves.VPG2.Pin1", "Consumers.FB.Input");
		var vpg1Link = Link("Root.Group.Valves.VPG1.Pin1", "Consumers.FB.Input2");

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(valvesDto, vpg2Link, vpg1Link),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "VPG1", "VPG2" });

		// VPG2 was constructed: its links should be filtered to VPG2 subtree only
		var vpg2Construction = constructions.SingleOrDefault(c => c.Path.EndsWith(".VPG2"));
		vpg2Construction.Path.Should().NotBeNull();
		vpg2Construction.Links.Should().ContainSingle()
			.Which.LocalPinPath.Should().Contain("VPG2");

		shrinkCount.Should().Be(0);
		disconnector.RecordedPaths.Should().BeEmpty();
	}

	// Case 4: nested shrink, Valves preserved, one child removed

	[Fact]
	public void NestedShrink_RemovesExcessChildFromPreservedNode()
	{
		var container = ScadaItem("Group",
			ScadaItem("Valves",
				ScadaItem("VPG1"),
				ScadaItem("VPG4")));

		var desired = new[]
		{
			Branch("Valves", Leaf("VPG1")),
		};

		var valvesDto = DtoNode("Valves", DtoNode("VPG1"), DtoNode("VPG4"));
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(valvesDto),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Select(i => i.Name).Should().Equal("VPG1");

		shrinkCount.Should().Be(1);
		disconnector.RecordedPaths.Should().ContainSingle(p => p.EndsWith(".VPG4"));
		constructions.Should().BeEmpty();
	}

	// Case 5: pruned construction, group missing from current, spec restricts children

	[Fact]
	public void PrunedConstruction_GroupMissing_ConstructsOnlySpecifiedChildren()
	{
		var container = ScadaItem("Group"); // Valves not present

		var desired = new[]
		{
			Branch("Valves", Leaf("VPG1"), Leaf("VPG2")),
		};

		// Snapshot has 5 VPG children; spec restricts to 2
		var valvesDto = DtoNode("Valves",
			DtoNode("VPG1"),
			DtoNode("VPG2"),
			DtoNode("VPG3"),
			DtoNode("VPG4"),
			DtoNode("VPG5"));

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(valvesDto),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Should().HaveCount(2);
		valvesItem.Items.Select(i => i.Name).Should().Equal("VPG1", "VPG2");

		constructions.Should().ContainSingle(c => c.Path.EndsWith(".Valves"));
		shrinkCount.Should().Be(0);
	}

	// Case 6: $-only link survives dedup + filter

	[Fact]
	public void DollarLink_SurvivesFilterAndAppearsOnceInConstruction()
	{
		var container = ScadaItem("Group"); // Command missing

		var desired = new[] { Leaf("Command") };

		var commandDto = DtoNode("Command", DtoNode("ControlWord$"));

		var dollarLink = new LinkEntry
		{
			LocalPinPath = "Root.Group.Command.ControlWord$.Value",
			ExternalPinPath = "CMD.Result",
			LinkType = LinkTypes.DirectPin,
		};

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Command"] = Snapshot(commandDto, dollarLink),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Should().ContainSingle(i => i.Name == "Command");
		constructions.Should().ContainSingle(c => c.Path.EndsWith(".Command"));

		var commandConstruction = constructions.Single(c => c.Path.EndsWith(".Command"));
		commandConstruction.Links.Should().ContainSingle()
			.Which.LocalPinPath.Should().Be("Root.Group.Command.ControlWord$.Value");
	}

	// Case 7: desired node absent from both current container and snapshot

	[Fact]
	public void NodeAbsentFromCurrentAndSnapshot_IsSilentlySkipped()
	{
		var container = ScadaItem("Group", ScadaItem("A"));

		// B is desired but has no snapshot entry and is not present in the container.
		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		// A is preserved; B is silently skipped because it is absent from both current and snapshot.
		container.Items.Select(i => i.Name).Should().Equal("A");
		shrinkCount.Should().Be(0);
		disconnector.RecordedPaths.Should().BeEmpty();
		constructions.Should().BeEmpty();
	}

	[Fact]
	public void ApplyDesiredSpec_EmptyContainer_ConstructsEveryDesiredNodeWithItsLinks()
	{
		var container = ScadaItem("Group");

		var desired = new[] { Leaf("A"), Branch("Valves", Leaf("VPG1")), Leaf("Command") };

		var valvesLink = Link("Root.Group.Valves.VPG1", "Root.Valves.VPG1.State");
		var legacyLink = Link("Root.Group.Legacy", "Root.Legacy.State");

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["Valves"] = Snapshot(DtoNode("Valves", DtoNode("VPG1"), DtoNode("VPG2")), valvesLink),
			["Command"] = Snapshot(DtoNode("Command")),
			["Legacy"] = Snapshot(DtoNode("Legacy"), legacyLink),
		};

		var disconnector = MakeDisconnector();
		Invoke(disconnector, container, desired, "Root.Group", snapshot,
			out var constructions, out _);

		container.Items.Select(i => i.Name).Should().Equal("A", "Valves", "Command");
		constructions.Should().HaveCount(desired.Length);
		constructions.Should().NotContain(c => c.Path.EndsWith(".Legacy"));

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Select(i => i.Name).Should().Equal("VPG1");

		var allLinks = constructions.SelectMany(c => c.Links).ToList();
		allLinks.Should().Contain(valvesLink, "a reconnect needs the link the snapshot recorded");
		allLinks.Should().NotContain(legacyLink, "a node the spec dropped brings no links with it");
	}

	[Fact]
	public void PlannedRebuild_EmptyContainer_ReshapesFromTheBuiltPlan()
	{
		var container = ScadaItem("Group");

		var config = new OpcConfig
		{
			Projects = new Dictionary<string, List<NodeSpec>>
			{
				["MBE"] = new List<NodeSpec> { Leaf("A"), Branch("Valves", Leaf("VPG1")) },
			},
		};

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["Valves"] = Snapshot(DtoNode("Valves", DtoNode("VPG1"), DtoNode("VPG2"))),
		};

		var planResult = PlanBuilder.Build(
			opcFbPath: "Root",
			groupName: "Group",
			targetProject: "MBE",
			config: config,
			snapshot: snapshot,
			currentTopLevelNames: Array.Empty<string>());

		planResult.IsSuccess.Should().BeTrue();

		var plan = planResult.Value!;
		var disconnector = MakeDisconnector();

		Invoke(disconnector, container, plan.DesiredTree, "Root.Group", plan.Snapshot,
			out var constructions, out _);

		container.Items.Select(i => i.Name).Should().Equal("A", "Valves");
		constructions.Should().HaveCount(2, "a plan that builds must also construct");
	}

	[Fact]
	public void Reshape_NodeConstructedWithLinks_LogsOneInformationPerNode()
	{
		var container = ScadaItem("Group");

		var desired = new[] { Leaf("A") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A"), Link("Root.Group.A.Pin1", "Consumers.FB.Input")),
		};

		var sink = ReshapeCapturingLog(MakeDisconnector(), container, desired, "Root.Group", snapshot);

		var constructed = EventsOf(sink, "Constructed '{NodePath}': {LinkCount} links to connect").Single();

		constructed.Level.Should().Be(LogEventLevel.Information);
		CapturingSink.Property(constructed, "NodePath").Should().Be("Root.Group.A");
		CapturingSink.Property(constructed, "LinkCount").Should().Be("1");
	}

	[Fact]
	public void Reshape_NodeConstructedWithoutLinks_IsRecordedAtErrorAndCounted()
	{
		var container = ScadaItem("Group");

		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A"), Link("Root.Group.A.Pin1", "Consumers.FB.Input")),
			["B"] = Snapshot(DtoNode("B")),
		};

		var sink = new CapturingSink();

		var result = TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			MakeDisconnector(),
			() => { },
			sink.ToLogger());

		var unwired = EventsOf(
			sink,
			"Constructed '{NodePath}': no links to connect; the snapshot holds none under the "
			+ "kept nodes, so the node comes back unwired").Single();

		unwired.Level.Should().Be(LogEventLevel.Error);
		CapturingSink.Property(unwired, "NodePath").Should().Be("Root.Group.B");
		result.Constructions.Count(c => c.Links.Count == 0)
			.Should().Be(1, "only B came back with no links");
		result.Constructions.Should().HaveCount(2, "an unwired node is still constructed");
	}

	[Fact]
	public void Reshape_ConstructOnlyPlan_FlagsTheMutationBeforeTheSwapAndNotEarlier()
	{
		var container = ScadaItem("Group");

		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["B"] = Snapshot(DtoNode("B")),
		};

		var itemCountsAtFlag = new List<int>();

		TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			MakeDisconnector(),
			() => itemCountsAtFlag.Add(container.Items.Count),
			Serilog.Core.Logger.None);

		container.Items.Should().HaveCount(2);
		itemCountsAtFlag.Should().Equal(
			new[] { 0 },
			"the construction loop only reads; the swap is the first write");
	}

	[Fact]
	public void Reshape_ContainerAlreadyMatchesTheSpec_NeverFlagsAMutation()
	{
		var container = ScadaItem("Group", ScadaItem("A"), ScadaItem("B"));

		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["B"] = Snapshot(DtoNode("B")),
		};

		var flagged = false;

		TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			MakeDisconnector(),
			() => flagged = true,
			Serilog.Core.Logger.None);

		flagged.Should().BeFalse("a run that swapped nothing left the tree untouched");
	}

	[Fact]
	public void Reshape_RemovalDisconnects_FlagsTheMutationFromTheDisconnectorFirst()
	{
		var container = ScadaItem("Group", ScadaItem("Valves"), ScadaItem("Legacy"));

		var desired = new[] { Leaf("Valves") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(DtoNode("Valves")),
		};

		var itemCountsAtFlag = new List<int>();

		TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			MakeDisconnector().Returns(".Legacy", issued: 7, threw: 0, nodesMissing: 0),
			() => itemCountsAtFlag.Add(container.Items.Count),
			Serilog.Core.Logger.None);

		itemCountsAtFlag.Should().HaveCountGreaterThan(0, "a disconnect was issued");
		itemCountsAtFlag[0].Should().Be(2, "the first flag came from the disconnect, before the swap");
	}

	[Fact]
	public void Reshape_NestedContainers_ReshapedLineCountsPerContainerNotRunningTotal()
	{
		var container = ScadaItem("Group",
			ScadaItem("Valves", ScadaItem("VPG1"), ScadaItem("VPG4")),
			ScadaItem("Legacy"));

		var desired = new[] { Branch("Valves", Leaf("VPG1")), Leaf("Ghost") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(DtoNode("Valves", DtoNode("VPG1"), DtoNode("VPG4"))),
		};

		var disconnector = MakeDisconnector();
		var sink = ReshapeCapturingLog(disconnector, container, desired, "Root.Group", snapshot);

		var reshaped = EventsOf(
			sink,
			"Reshaped '{ContainerPath}': removed={RemovedCount} constructed={ConstructedCount} "
			+ "preserved={PreservedCount} skipped={SkippedCount}");

		reshaped.Should().HaveCount(2);
		reshaped.Should().OnlyContain(e => e.Level == LogEventLevel.Information);

		var inner = reshaped.Single(e => CapturingSink.Property(e, "ContainerPath") == "Root.Group.Valves");
		CapturingSink.Property(inner, "RemovedCount").Should().Be("1");
		CapturingSink.Property(inner, "ConstructedCount").Should().Be("0");
		CapturingSink.Property(inner, "PreservedCount").Should().Be("1");
		CapturingSink.Property(inner, "SkippedCount").Should().Be("0");

		var outer = reshaped.Single(e => CapturingSink.Property(e, "ContainerPath") == "Root.Group");
		CapturingSink.Property(outer, "RemovedCount").Should().Be("1");
		CapturingSink.Property(outer, "ConstructedCount").Should().Be("0");
		CapturingSink.Property(outer, "PreservedCount").Should().Be("1");
		CapturingSink.Property(outer, "SkippedCount").Should().Be("1");

		var disconnects = EventsOf(
			sink,
			"Removed '{NodePath}': {IssuedCount} disconnects issued, {ThrewCount} threw");

		disconnects.Should().OnlyContain(e => e.Level == LogEventLevel.Information);
		disconnects.Select(e => CapturingSink.Property(e, "NodePath"))
			.Should().BeEquivalentTo("Root.Group.Legacy", "Root.Group.Valves.VPG4");
	}

	[Fact]
	public void Reshape_RemovedNodes_AccumulateTheDisconnectorTallyAndRecordEachNode()
	{
		var container = ScadaItem("Group", ScadaItem("Valves"), ScadaItem("Legacy"));

		var desired = new[] { Leaf("Valves") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(DtoNode("Valves")),
		};

		var disconnector = MakeDisconnector().Returns(".Legacy", issued: 7, threw: 2, nodesMissing: 0);
		var sink = ReshapeCapturingLog(disconnector, container, desired, "Root.Group", snapshot);

		var removed = EventsOf(
			sink,
			"Removed '{NodePath}': {IssuedCount} disconnects issued, {ThrewCount} threw").Single();

		CapturingSink.Property(removed, "NodePath").Should().Be("Root.Group.Legacy");
		CapturingSink.Property(removed, "IssuedCount").Should().Be("7");
		CapturingSink.Property(removed, "ThrewCount").Should().Be("2");
	}

	[Fact]
	public void Reshape_RemovedNodeMissingFromProject_WritesNoRemovalRecordAndCountsItMissing()
	{
		var container = ScadaItem("Group", ScadaItem("Valves"), ScadaItem("Legacy"));

		var desired = new[] { Leaf("Valves") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Valves"] = Snapshot(DtoNode("Valves")),
		};

		var disconnector = MakeDisconnector().Returns(".Legacy", issued: 0, threw: 0, nodesMissing: 1);
		var sink = new CapturingSink();

		var result = TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			disconnector,
			() => { },
			sink.ToLogger());

		EventsOf(sink, "Removed '{NodePath}': {IssuedCount} disconnects issued, {ThrewCount} threw")
			.Should().BeEmpty();

		result.NodesMissing.Should().Be(1);
		result.DisconnectsThrew.Should().Be(0, "a missing node is not a failed disconnect");
	}

	[Fact]
	public void Reshape_NestedNodeAbsentFromContainerAndSnapshot_IsRecordedAtError()
	{
		var container = ScadaItem("Group", ScadaItem("Valves", ScadaItem("VPG1")));

		var desired = new[] { Branch("Valves", Leaf("VPG1"), Leaf("VPG2")) };

		// Valves is in the container but absent from the snapshot, so its children resolve to a null DTO.
		var snapshot = new Dictionary<string, NodeSnapshot>();

		var sink = ReshapeCapturingLog(MakeDisconnector(), container, desired, "Root.Group", snapshot);

		var notRestored = EventsOf(
			sink,
			"Not restored '{NodePath}': not in the container, not in the snapshot").Single();

		notRestored.Level.Should().Be(LogEventLevel.Error);
		CapturingSink.Property(notRestored, "NodePath").Should().Be("Root.Group.Valves.VPG2");
	}

	[Fact]
	public void Reshape_TopLevelNodeAbsentFromContainerAndSnapshot_StaysAtDebug()
	{
		var container = ScadaItem("Group", ScadaItem("A"));

		var desired = new[] { Leaf("A"), Leaf("Ghost") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
		};

		var sink = ReshapeCapturingLog(MakeDisconnector(), container, desired, "Root.Group", snapshot);

		var notRestored = EventsOf(
			sink,
			"Not restored '{NodePath}': not in the container, not in the snapshot").Single();

		notRestored.Level.Should().Be(LogEventLevel.Debug, "the plan-time Error already names a top-level node");
		CapturingSink.Property(notRestored, "NodePath").Should().Be("Root.Group.Ghost");
	}

	[Fact]
	public void Reshape_NodesSkippedAtBothLevels_AreCountedOntoTheResult()
	{
		var container = ScadaItem("Group", ScadaItem("Valves", ScadaItem("VPG1")));

		var desired = new[] { Branch("Valves", Leaf("VPG1"), Leaf("VPG2")), Leaf("Ghost") };
		var snapshot = new Dictionary<string, NodeSnapshot>();

		var result = TreeReshaper.Reshape(
			container,
			desired,
			"Root.Group",
			name => snapshot.TryGetValue(name, out var s)
				? (s.ScadaItem, s.Links)
				: (null, Array.Empty<LinkEntry>()),
			MakeDisconnector(),
			() => { },
			new CapturingSink().ToLogger());

		result.NodesNotRestored.Should().Be(2, "Ghost at the top level and VPG2 under Valves");
	}

	[Fact]
	public void Reshape_ContainerAlreadyMatchesTheSpec_WritesNoReshapedLine()
	{
		var container = ScadaItem("Group", ScadaItem("A"), ScadaItem("B"));

		var desired = new[] { Leaf("A"), Leaf("B") };
		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["A"] = Snapshot(DtoNode("A")),
			["B"] = Snapshot(DtoNode("B")),
		};

		var sink = ReshapeCapturingLog(MakeDisconnector(), container, desired, "Root.Group", snapshot);

		EventsOf(
			sink,
			"Reshaped '{ContainerPath}': removed={RemovedCount} constructed={ConstructedCount} "
			+ "preserved={PreservedCount} skipped={SkippedCount}")
			.Should().BeEmpty("a container whose items were not swapped was not reshaped");
	}
}
