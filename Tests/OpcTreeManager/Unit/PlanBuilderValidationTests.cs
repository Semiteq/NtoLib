using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.Facade;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

/// <summary>
/// Plan-time resolvability validation in <see cref="PlanBuilder.Build"/>: a desired node that
/// does not resolve against the snapshot must fail the plan before anything mutates, instead of
/// letting the executor throw mid-rebuild (after removed subtrees are already disconnected).
/// </summary>
public sealed class PlanBuilderValidationTests
{
	private const string Project = "MBE";
	private const string OpcFbPath = "Project.OpcUaFB";
	private const string GroupName = "TestGroup";

	[Fact]
	public void Build_NestedChildAbsentFromSnapshot_FailsWithNoPlan()
	{
		// Desired asks for CH1.Setpoint, but the snapshot's CH1 has no Setpoint child.
		var config = ConfigFor(
			new NodeSpec("TemperatureControllers", new[]
			{
				new NodeSpec("CH1", new[] { new NodeSpec("Setpoint", null) }),
			}));

		var snapshot = SnapshotOf(
			Node("TemperatureControllers",
				Node("CH1")));

		var result = PlanBuilder.Build(
			opcFbPath: OpcFbPath,
			groupName: GroupName,
			targetProject: Project,
			config: config,
			snapshot: snapshot,
			currentTopLevelNames: new List<string>());

		result.IsFailed.Should().BeTrue("a nested desired child absent from the snapshot is unbuildable");
		string.Join(";", result.Errors).Should()
			.Contain("TestGroup.TemperatureControllers.CH1.Setpoint",
				"the failure names the offending path");
	}

	[Fact]
	public void Build_TopLevelNodeAbsentFromSnapshot_StillBuilds()
	{
		// A top-level node absent from the snapshot resolves to a null DTO and is
		// skipped-with-warning at execute time — never pruned, so no mid-rebuild throw.
		// Plan-time validation must NOT fail it (only the nested prune-throw is a hazard).
		var config = ConfigFor(
			new NodeSpec("Valves", null));

		var snapshot = SnapshotOf(
			Node("TemperatureControllers", Node("CH1")));

		var result = PlanBuilder.Build(
			opcFbPath: OpcFbPath,
			groupName: GroupName,
			targetProject: Project,
			config: config,
			snapshot: snapshot,
			currentTopLevelNames: new List<string>());

		result.IsSuccess.Should().BeTrue("a top-level node absent from the snapshot is the safe skip path");
		result.Value.Should().NotBeNull("a rebuild plan is still produced");
	}

	[Fact]
	public void Build_FullyResolvableDesiredTree_BuildsPlan()
	{
		var config = ConfigFor(
			new NodeSpec("TemperatureControllers", new[]
			{
				new NodeSpec("CH1", new[] { new NodeSpec("Setpoint", null) }),
			}));

		var snapshot = SnapshotOf(
			Node("TemperatureControllers",
				Node("CH1",
					Node("Setpoint"),
					Node("Actual"))));

		var result = PlanBuilder.Build(
			opcFbPath: OpcFbPath,
			groupName: GroupName,
			targetProject: Project,
			config: config,
			snapshot: snapshot,
			currentTopLevelNames: new List<string>());

		result.IsSuccess.Should().BeTrue("every desired node resolves against the snapshot");
		result.Value.Should().NotBeNull("a non-leaf desired tree produces a real plan (no short-circuit)");
		result.Value!.DesiredTree.Select(n => n.Name).Should().Equal("TemperatureControllers");
	}

	private static OpcConfig ConfigFor(params NodeSpec[] desired)
	{
		return new OpcConfig
		{
			Projects = new Dictionary<string, List<NodeSpec>>
			{
				[Project] = desired.ToList(),
			},
		};
	}

	private static IReadOnlyDictionary<string, NodeSnapshot> SnapshotOf(params OpcScadaItemDto[] topLevel)
	{
		return topLevel.ToDictionary(
			dto => dto.Name,
			dto => new NodeSnapshot { ScadaItem = dto });
	}

	private static OpcScadaItemDto Node(string name, params OpcScadaItemDto[] children)
	{
		return new OpcScadaItemDto
		{
			Name = name,
			PinValueType = "0",
			DeadbandType = "None",
			Items = children.ToList(),
		};
	}
}
