using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using OpcUaClient.Client.Common.Data;

using Tests.OpcTreeManager.Integration.Fakes;

using Xunit;

namespace Tests.OpcTreeManager.Integration;

/// <summary>
/// Seam-based tests for <see cref="PlanExecutor.TestApplyDesiredSpec"/>.
/// All vendor COM calls are replaced by <see cref="FakeSubtreeDisconnector"/>.
/// </summary>
public sealed class ApplyDesiredSpecTests
{
	// ──────────────────────────────────────────────────────────────────────
	//  Helpers
	// ──────────────────────────────────────────────────────────────────────

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

	private static (FakeSubtreeDisconnector Disconnector, PlanExecutor Executor) MakeExecutor()
	{
		var disconnector = new FakeSubtreeDisconnector();
		var executor = new PlanExecutor(project: null!, disconnector, Serilog.Core.Logger.None);
		return (disconnector, executor);
	}

	private static void Invoke(
		PlanExecutor executor,
		OpcUaScadaItem container,
		IReadOnlyList<NodeSpec> desired,
		string containerPath,
		IReadOnlyDictionary<string, NodeSnapshot> snapshot,
		out List<PlanExecutor.Construction> constructions,
		out int shrinkCount)
	{
		executor.TestApplyDesiredSpec(container, desired, containerPath, snapshot,
			out constructions, out shrinkCount);
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 1: single-level shrink
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Select(i => i.Name).Should().Equal("A", "B");
		shrinkCount.Should().Be(1);
		disconnector.RecordedPaths.Should().ContainSingle(p => p.EndsWith(".C"));
		constructions.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 2: single-level expand — new node present in snapshot
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Select(i => i.Name).Should().Equal("A", "B");
		shrinkCount.Should().Be(0);
		disconnector.RecordedPaths.Should().BeEmpty();
		constructions.Should().ContainSingle();
		constructions[0].Path.Should().EndWith(".B");
		constructions[0].Links.Should().HaveCount(1);
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 3: nested expand — Valves preserved, one new child added
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
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

	// ──────────────────────────────────────────────────────────────────────
	//  Case 4: nested shrink — Valves preserved, one child removed
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Select(i => i.Name).Should().Equal("VPG1");

		shrinkCount.Should().Be(1);
		disconnector.RecordedPaths.Should().ContainSingle(p => p.EndsWith(".VPG4"));
		constructions.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 5: pruned construction — group missing from current, spec restricts children
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		var valvesItem = container.Items.Single(i => i.Name == "Valves");
		valvesItem.Items.Should().HaveCount(2);
		valvesItem.Items.Select(i => i.Name).Should().Equal("VPG1", "VPG2");

		constructions.Should().ContainSingle(c => c.Path.EndsWith(".Valves"));
		shrinkCount.Should().Be(0);
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 6: $-only link survives dedup + filter
	// ──────────────────────────────────────────────────────────────────────

	[Fact]
	public void DollarLink_SurvivesFilterAndAppearsOnceInConstruction()
	{
		var container = ScadaItem("Group"); // Command missing

		var desired = new[] { Leaf("Command") };

		var commandDto = DtoNode("Command", DtoNode("ControlWord$"));

		var dollarLink = new LinkEntry
		{
			LocalPinPath = "Root.Group.Command.ControlWord$.Value",
			ExternalPinPath = "CMD.Результат",
			LinkType = LinkTypes.DirectPin,
		};

		var snapshot = new Dictionary<string, NodeSnapshot>
		{
			["Command"] = Snapshot(commandDto, dollarLink),
		};

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		container.Items.Should().ContainSingle(i => i.Name == "Command");
		constructions.Should().ContainSingle(c => c.Path.EndsWith(".Command"));

		var commandConstruction = constructions.Single(c => c.Path.EndsWith(".Command"));
		commandConstruction.Links.Should().ContainSingle()
			.Which.LocalPinPath.Should().Be("Root.Group.Command.ControlWord$.Value");
	}

	// ──────────────────────────────────────────────────────────────────────
	//  Case 7: desired node absent from both current container and snapshot
	// ──────────────────────────────────────────────────────────────────────

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

		var (disconnector, executor) = MakeExecutor();
		Invoke(executor, container, desired, "Root.Group", snapshot,
			out var constructions, out var shrinkCount);

		// A is preserved; B is silently skipped because it is absent from both current and snapshot.
		container.Items.Select(i => i.Name).Should().Equal("A");
		shrinkCount.Should().Be(0);
		disconnector.RecordedPaths.Should().BeEmpty();
		constructions.Should().BeEmpty();
	}
}
