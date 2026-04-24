using System;
using System.IO;

using FluentAssertions;

using NtoLib.OpcTreeManager.Config;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class OpcConfigLoaderTests
{
	[Fact]
	public void Load_FlatList_ParsesAsAllLeaves()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - Cameras
    - Axes
    - Valves
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		var specs = result.Value.Projects["MBE"];
		specs.Should().HaveCount(3);
		specs.Should().OnlyContain(s => s.Children == null);
		specs.Select(s => s.Name).Should().Equal("Cameras", "Axes", "Valves");
	}

	[Fact]
	public void Load_NestedNonLeaf_HasChildrenPopulated()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - Cameras
    - Valves:
        - VPG1
        - VPG2
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		var specs = result.Value.Projects["MBE"];
		specs.Should().HaveCount(2);
		specs[0].Name.Should().Be("Cameras");
		specs[0].Children.Should().BeNull();
		specs[1].Name.Should().Be("Valves");
		specs[1].Children.Should().NotBeNull();
		specs[1].Children!.Select(c => c.Name).Should().Equal("VPG1", "VPG2");
		specs[1].Children!.Should().OnlyContain(c => c.Children == null);
	}

	[Fact]
	public void Load_DeeplyNested_BuildsFullTree()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - TemperatureControllers:
        - CH1:
            - Setpoint
        - CH2
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		var tc = result.Value.Projects["MBE"][0];
		tc.Name.Should().Be("TemperatureControllers");
		tc.Children.Should().NotBeNull().And.HaveCount(2);

		var ch1 = tc.Children![0];
		ch1.Name.Should().Be("CH1");
		ch1.Children.Should().NotBeNull();
		ch1.Children!.Single().Name.Should().Be("Setpoint");
		ch1.Children![0].Children.Should().BeNull();

		var ch2 = tc.Children[1];
		ch2.Name.Should().Be("CH2");
		ch2.Children.Should().BeNull();
	}

	[Fact]
	public void Load_EmptyChildrenValue_ParsesAsEmptyList()
	{
		// `- Name:` with no following list -> explicit empty children list.
		using var file = TempYaml(@"
projects:
  MBE:
    - EmptyNode:
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		var spec = result.Value.Projects["MBE"].Single();
		spec.Name.Should().Be("EmptyNode");
		spec.Children.Should().NotBeNull().And.BeEmpty();
	}

	[Fact]
	public void Load_MultiKeyMapping_ReturnsFail()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - Valves:
        - VPG1
      Pumps:
        - NR1
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("exactly one key");
	}

	[Fact]
	public void Load_ChildrenAsScalar_ReturnsFail()
	{
		// `- Name: foo` is malformed for this schema — value must be a list.
		using var file = TempYaml(@"
projects:
  MBE:
    - Valves: notAListButAScalar
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("must be a list");
	}

	[Fact]
	public void Load_FoldedScalarWithSpaces_ReturnsFail()
	{
		// A folded YAML scalar like `- "Valves - VPG1"` or `- Valves - VPG1`
		// is a common user mistake. The parser would accept it as a plain string,
		// but it is semantically wrong — should be a mapping with children.
		using var file = TempYaml(@"
projects:
  MBE:
    - ""Valves - VPG1""
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("whitespace");
	}

	[Fact]
	public void Load_SingleWordScalar_Succeeds()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - Valves
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		result.Value.Projects["MBE"].Single().Name.Should().Be("Valves");
	}

	[Fact]
	public void Load_NameWithDot_Succeeds()
	{
		// OPC UA node names can contain dots — ensure dots are not rejected.
		using var file = TempYaml(@"
projects:
  MBE:
    - Valve.VPG1
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsSuccess.Should().BeTrue();
		result.Value.Projects["MBE"].Single().Name.Should().Be("Valve.VPG1");
	}

	[Fact]
	public void Load_FileNotFound_ReturnsFail()
	{
		var result = OpcConfigLoader.Load(@"C:\NonExistent\missing.yaml");

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("not found");
	}

	[Fact]
	public void Load_EmptyScalarName_ReturnsFail()
	{
		// Empty-quoted scalar would pass the whitespace check but leave a blank name
		// that would flow downstream and corrupt the plan.
		using var file = TempYaml(@"
projects:
  MBE:
    - """"
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("Empty node name");
	}

	[Fact]
	public void Load_DuplicateSiblings_ReturnsFail()
	{
		using var file = TempYaml(@"
projects:
  MBE:
    - Valves
    - Valves
");

		var result = OpcConfigLoader.Load(file.Path);

		result.IsFailed.Should().BeTrue();
		string.Join(";", result.Errors).Should().Contain("Duplicate sibling name");
	}

	private static TempYamlFile TempYaml(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"opcconfig-{Guid.NewGuid():N}.yaml");
		File.WriteAllText(path, content);
		return new TempYamlFile(path);
	}

	private sealed class TempYamlFile : IDisposable
	{
		public string Path { get; }

		public TempYamlFile(string path) => Path = path;

		public void Dispose()
		{
			try
			{
				if (File.Exists(Path))
				{
					File.Delete(Path);
				}
			}
			catch
			{
				// best-effort cleanup
			}
		}
	}
}
