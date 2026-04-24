using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;
using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.Facade;

using Xunit;

namespace Tests.OpcTreeManager.Acceptance;

/// <summary>
/// Fixture-driven acceptance tests for <see cref="PlanBuilder"/>.
/// Each case lives under Tests/OpcTreeManager/Fixtures/Acceptance/&lt;case-name&gt;/
/// as three files: config.yaml, tree.json, expected.json.
/// </summary>
public sealed class PlanBuilderAcceptanceTests
{
	private static readonly string FixturesRoot = FindFixturesRoot();

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	/// <summary>Discovers all fixture case names to feed into the theory.</summary>
	public static IEnumerable<object[]> Cases()
	{
		if (!Directory.Exists(FixturesRoot))
		{
			yield break;
		}

		foreach (var dir in Directory.EnumerateDirectories(FixturesRoot))
		{
			yield return new object[] { Path.GetFileName(dir) };
		}
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public void PlanBuilder_FixtureCase(string caseName)
	{
		var caseDir = Path.Combine(FixturesRoot, caseName);
		var configPath = Path.Combine(caseDir, "config.yaml");
		var treePath = Path.Combine(caseDir, "tree.json");
		var expectedPath = Path.Combine(caseDir, "expected.json");

		var expectedJson = File.ReadAllText(expectedPath);
		var expected = JsonSerializer.Deserialize<ExpectedOutcome>(expectedJson, JsonOptions)!;

		var configResult = OpcConfigLoader.Load(configPath);
		var snapshotResult = TreeSnapshotLoader.Load(treePath);

		if (!expected.IsOk)
		{
			// Expected failure — either config load or plan build should fail.
			if (configResult.IsFailed)
			{
				var errors = string.Join(";", configResult.Errors);
				if (expected.ErrorSubstring != null)
				{
					errors.Should().Contain(expected.ErrorSubstring,
						because: $"case '{caseName}': config error should contain expected substring");
				}

				return;
			}

			configResult.IsSuccess.Should().BeTrue(
				because: $"case '{caseName}': config should load for a plan-level failure test");

			snapshotResult.IsSuccess.Should().BeTrue(
				because: $"case '{caseName}': snapshot should load for a plan-level failure test");

			// Use targetProject from the fixture if specified; otherwise default to a value that
			// is guaranteed absent from any real config, so the plan-level failure is triggered
			// only for cases that explicitly rely on a missing project name.
			var targetProjectForFailure = expected.TargetProject ?? "NONEXISTENT_PROJECT";
			var failPlan = PlanBuilder.Build(
				opcFbPath: "Project.OpcUaFB",
				groupName: "TestGroup",
				targetProject: targetProjectForFailure,
				config: configResult.Value,
				snapshot: snapshotResult.Value,
				currentTopLevelNames: new List<string>());

			failPlan.IsFailed.Should().BeTrue(
				because: $"case '{caseName}': PlanBuilder should fail");

			if (expected.ErrorSubstring != null)
			{
				var planErrors = string.Join(";", failPlan.Errors);
				planErrors.Should().Contain(expected.ErrorSubstring,
					because: $"case '{caseName}': plan error should contain expected substring");
			}

			return;
		}

		// Expected success path
		configResult.IsSuccess.Should().BeTrue(
			because: $"case '{caseName}': config load should succeed");

		snapshotResult.IsSuccess.Should().BeTrue(
			because: $"case '{caseName}': snapshot load should succeed");

		var currentNames = snapshotResult.Value.Keys.ToList();

		var planResult = PlanBuilder.Build(
			opcFbPath: "Project.OpcUaFB",
			groupName: "TestGroup",
			targetProject: "MBE",
			config: configResult.Value,
			snapshot: snapshotResult.Value,
			currentTopLevelNames: currentNames);

		planResult.IsSuccess.Should().BeTrue(
			because: $"case '{caseName}': PlanBuilder should succeed");

		if (expected.IsNull)
		{
			planResult.Value.Should().BeNull(
				because: $"case '{caseName}': short-circuit expected — plan should be null");
			return;
		}

		planResult.Value.Should().NotBeNull(
			because: $"case '{caseName}': a non-null plan is expected");

		var plan = planResult.Value!;

		if (expected.DesiredTreeTopLevelNames != null)
		{
			plan.DesiredTree.Select(n => n.Name).Should()
				.BeEquivalentTo(expected.DesiredTreeTopLevelNames,
					because: $"case '{caseName}': desired tree top-level names should match");
		}

		if (expected.SnapshotKeys != null)
		{
			plan.Snapshot.Keys.Should()
				.BeEquivalentTo(expected.SnapshotKeys,
					because: $"case '{caseName}': snapshot keys should match");
		}

		if (expected.DesiredValvesChildNames != null)
		{
			var valvesSpec = plan.DesiredTree.FirstOrDefault(n => n.Name == "Valves");
			valvesSpec.Should().NotBeNull(
				because: $"case '{caseName}': Valves entry expected in desired tree");
			valvesSpec!.Children.Should().NotBeNull(
				because: $"case '{caseName}': Valves should have children");
			valvesSpec.Children!.Select(c => c.Name).Should()
				.BeEquivalentTo(expected.DesiredValvesChildNames,
					because: $"case '{caseName}': Valves children should match");
		}
	}

	private static string FindFixturesRoot()
	{
		var dir = AppContext.BaseDirectory;

		for (var i = 0; i < 12 && !string.IsNullOrEmpty(dir); i++)
		{
			var probe = Path.Combine(dir, "OpcTreeManager", "Fixtures", "Acceptance");

			if (Directory.Exists(probe))
			{
				return probe;
			}

			dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
		}

		throw new DirectoryNotFoundException(
			"Acceptance fixtures root not found. Expected 'OpcTreeManager/Fixtures/Acceptance' up the directory tree.");
	}

	/// <summary>
	/// Deserialised shape of <c>expected.json</c>.
	/// </summary>
	private sealed class ExpectedOutcome
	{
		[JsonPropertyName("isOk")]
		public bool IsOk { get; set; }

		[JsonPropertyName("isNull")]
		public bool IsNull { get; set; }

		[JsonPropertyName("errorSubstring")]
		public string? ErrorSubstring { get; set; }

		[JsonPropertyName("desiredTreeTopLevelNames")]
		public List<string>? DesiredTreeTopLevelNames { get; set; }

		[JsonPropertyName("snapshotKeys")]
		public List<string>? SnapshotKeys { get; set; }

		[JsonPropertyName("desiredValvesChildNames")]
		public List<string>? DesiredValvesChildNames { get; set; }

		[JsonPropertyName("targetProject")]
		public string? TargetProject { get; set; }
	}
}
