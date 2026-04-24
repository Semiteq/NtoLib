using System.Collections.Generic;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class OpcScadaItemDtoPruneTests
{
	[Fact]
	public void ToScadaItemPruned_NullSpec_ReturnsFullSubtree()
	{
		var dto = Node("Valves",
			Node("VPG1"),
			Node("VPG2"),
			Node("VPG4"));

		var item = dto.ToScadaItemPruned(spec: null);

		item.Name.Should().Be("Valves");
		item.Items.Should().HaveCount(3);
		item.Items.Select(i => i.Name).Should().Equal("VPG1", "VPG2", "VPG4");
	}

	[Fact]
	public void ToScadaItemPruned_LeafSpec_ReturnsFullSubtree()
	{
		var dto = Node("Valves", Node("VPG1"), Node("VPG4"));
		var leafSpec = new NodeSpec("Valves", Children: null);

		var item = dto.ToScadaItemPruned(leafSpec);

		item.Items.Should().HaveCount(2);
		item.Items.Select(i => i.Name).Should().Equal("VPG1", "VPG4");
	}

	[Fact]
	public void ToScadaItemPruned_NonLeafSpec_FiltersItemsByListedChildren()
	{
		var dto = Node("Valves", Node("VPG1"), Node("VPG2"), Node("VPG4"));
		var spec = new NodeSpec("Valves", new[]
		{
			new NodeSpec("VPG1", null),
			new NodeSpec("VPG2", null),
		});

		var item = dto.ToScadaItemPruned(spec);

		item.Items.Should().HaveCount(2);
		item.Items.Select(i => i.Name).Should().Equal("VPG1", "VPG2");
	}

	[Fact]
	public void ToScadaItemPruned_NonLeafSpec_RecursesIntoChildrenWithTheirSpecs()
	{
		var dto = Node("TemperatureControllers",
			Node("CH1",
				Node("Setpoint"),
				Node("Actual"),
				Node("MaxLimit")),
			Node("CH2",
				Node("Setpoint"),
				Node("Actual")));

		var spec = new NodeSpec("TemperatureControllers", new[]
		{
			new NodeSpec("CH1", new[] { new NodeSpec("Setpoint", null) }),
			new NodeSpec("CH2", null),  // leaf — keep all
		});

		var item = dto.ToScadaItemPruned(spec);

		var ch1 = item.Items.Single(i => i.Name == "CH1");
		ch1.Items.Should().ContainSingle(i => i.Name == "Setpoint");
		ch1.Items.Should().NotContain(i => i.Name == "Actual");
		ch1.Items.Should().NotContain(i => i.Name == "MaxLimit");

		var ch2 = item.Items.Single(i => i.Name == "CH2");
		ch2.Items.Select(i => i.Name).Should().Equal("Setpoint", "Actual");
	}

	[Fact]
	public void ToScadaItemPruned_ChildListedInSpecMissingFromSnapshot_Throws()
	{
		var dto = Node("Valves", Node("VPG1"));
		var spec = new NodeSpec("Valves", new[]
		{
			new NodeSpec("VPG1", null),
			new NodeSpec("VPG99", null),
		});

		var action = () => dto.ToScadaItemPruned(spec);

		action.Should().Throw<System.InvalidOperationException>()
			.WithMessage("*VPG99*");
	}

	private static OpcScadaItemDto Node(string name, params OpcScadaItemDto[] children)
	{
		return new OpcScadaItemDto
		{
			Name = name,
			PinValueType = "0",       // default PinType value
			DeadbandType = "None",
			Items = children.ToList(),
		};
	}
}
