using FluentAssertions;

using NtoLib.ConfigLoader.Entities;

using Tests.ConfigLoader.Helpers;

using Xunit;

namespace Tests.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "EdgeCases")]
public sealed class EdgeCaseTests
{
	[Fact]
	public void Load_EmptyYamlFile_ReturnsError()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		File.WriteAllText(filePath, "");

		var result = service.Load(filePath);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void Load_MalformedYaml_ReturnsError()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		File.WriteAllText(filePath, "not: valid: yaml: content:");

		var result = service.Load(filePath);

		result.IsFailed.Should().BeTrue();
	}

	[Fact]
	public void Load_YamlWithExtraGroups_IgnoresExtra()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareValidCase("Baseline");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var content = File.ReadAllText(filePath);
		content += "\nExtraGroup:\n  1: \"ExtraValue\"\n";
		File.WriteAllText(filePath, content);

		var result = service.Load(filePath);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public void Load_TwiceInSuccession_SecondLoadOverwritesFirst()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var dto1 = service.CreateEmptyConfiguration();
		dto1.Shutters[0] = "FirstValue";
		service.Save(filePath, dto1);
		service.Load(filePath);

		service.CurrentConfiguration.Shutters[0].Should().Be("FirstValue");

		var dto2 = service.CreateEmptyConfiguration();
		dto2.Shutters[0] = "SecondValue";
		service.Save(filePath, dto2);
		service.Load(filePath);

		service.CurrentConfiguration.Shutters[0].Should().Be("SecondValue");
	}

	[Fact]
	public void Save_OverwritesExistingFile()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var dto1 = service.CreateEmptyConfiguration();
		dto1.Shutters[0] = "Original";
		service.Save(filePath, dto1);

		var dto2 = service.CreateEmptyConfiguration();
		dto2.Shutters[0] = "Updated";
		service.Save(filePath, dto2);

		service.Load(filePath);
		service.CurrentConfiguration.Shutters[0].Should().Be("Updated");
	}

	[Fact]
	public void Save_AllGroupsPopulated_PreservesAll()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var shutters = CreateArrayWithPrefix(16, "Shutter");
		var sources = CreateArrayWithPrefix(32, "Source");
		var heaters = CreateArrayWithPrefix(16, "Heater");
		var water = CreateArrayWithPrefix(16, "Water");

		var dto = new LoaderDto(shutters, sources, heaters, water);

		var result = service.SaveAndReload(filePath, dto);

		result.IsSuccess.Should().BeTrue();

		for (var i = 0; i < 16; i++)
		{
			service.CurrentConfiguration.Shutters[i].Should().Be($"Shutter{i + 1}");
			service.CurrentConfiguration.ChamberHeaters[i].Should().Be($"Heater{i + 1}");
			service.CurrentConfiguration.WaterChannels[i].Should().Be($"Water{i + 1}");
		}

		for (var i = 0; i < 32; i++)
		{
			service.CurrentConfiguration.Sources[i].Should().Be($"Source{i + 1}");
		}
	}

	[Fact]
	public void CreateEmptyConfiguration_ReturnsCorrectArraySizes()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		var dto = service.CreateEmptyConfiguration();

		dto.Shutters.Length.Should().Be(16);
		dto.Sources.Length.Should().Be(32);
		dto.ChamberHeaters.Length.Should().Be(16);
		dto.WaterChannels.Length.Should().Be(16);
	}

	[Fact]
	public void Load_AfterFailedLoad_StateRemainsNotLoaded()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		var result = service.Load(@"Z:\NonExistent\Path\file.yaml");

		result.IsFailed.Should().BeTrue();
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().NotBeEmpty();
	}

	[Fact]
	public void Save_AfterFailedSave_LastErrorContainsDetails()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		var shutters = CreateEmptyArray(16);
		shutters[0] = "Invalid@Name";

		var dto = new LoaderDto(
			shutters,
			CreateEmptyArray(32),
			CreateEmptyArray(16),
			CreateEmptyArray(16));

		var result = service.Save(@"C:\ValidPath\file.yaml", dto);

		result.IsFailed.Should().BeTrue();
		service.LastError.Should().Contain("invalid characters");
	}

	private static string[] CreateEmptyArray(int size)
	{
		var array = new string[size];
		for (var i = 0; i < size; i++)
		{
			array[i] = string.Empty;
		}

		return array;
	}

	private static string[] CreateArrayWithPrefix(int size, string prefix)
	{
		var array = new string[size];
		for (var i = 0; i < size; i++)
		{
			array[i] = $"{prefix}{i + 1}";
		}

		return array;
	}
}
