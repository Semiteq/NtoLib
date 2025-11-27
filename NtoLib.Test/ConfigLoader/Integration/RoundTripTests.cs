using FluentAssertions;

using NtoLib.ConfigLoader.Entities;
using NtoLib.Test.ConfigLoader.Helpers;

using Xunit;

namespace NtoLib.Test.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "RoundTrip")]
public sealed class RoundTripTests
{
	[Fact]
	public void SaveAndReload_PreservesData()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var originalDto = CreateTestDto();

		var result = service.SaveAndReload(filePath, originalDto);

		result.IsSuccess.Should().BeTrue();
		service.CurrentConfiguration.Shutters[0].Should().Be("MyShutter");
		service.CurrentConfiguration.Shutters[1].Should().Be("Another Shutter");
		service.CurrentConfiguration.Sources[0].Should().Be("Source-1");
		service.CurrentConfiguration.ChamberHeaters[0].Should().Be("Heater.1");
		service.CurrentConfiguration.WaterChannels[0].Should().Be("Water_Pump");
	}

	[Fact]
	public void SaveAndReload_EmptyValues_PreservesEmpty()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var emptyDto = service.CreateEmptyConfiguration();

		var result = service.SaveAndReload(filePath, emptyDto);

		result.IsSuccess.Should().BeTrue();
		service.CurrentConfiguration.Shutters.Should().AllBe(string.Empty);
		service.CurrentConfiguration.Sources.Should().AllBe(string.Empty);
		service.CurrentConfiguration.ChamberHeaters.Should().AllBe(string.Empty);
		service.CurrentConfiguration.WaterChannels.Should().AllBe(string.Empty);
	}

	[Fact]
	public void SaveAndReload_SpecialCharacters_PreservesValues()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var shutters = CreateEmptyArray(16);
		shutters[0] = "Name With Spaces";
		shutters[1] = "Name.With.Dots";
		shutters[2] = "Name-With-Dashes";
		shutters[3] = "Name_With_Underscores";
		shutters[4] = "Mix 1.2-3_4";

		var dto = new LoaderDto(
			shutters,
			CreateEmptyArray(32),
			CreateEmptyArray(16),
			CreateEmptyArray(16));

		var result = service.SaveAndReload(filePath, dto);

		result.IsSuccess.Should().BeTrue();
		service.CurrentConfiguration.Shutters[0].Should().Be("Name With Spaces");
		service.CurrentConfiguration.Shutters[1].Should().Be("Name.With.Dots");
		service.CurrentConfiguration.Shutters[2].Should().Be("Name-With-Dashes");
		service.CurrentConfiguration.Shutters[3].Should().Be("Name_With_Underscores");
		service.CurrentConfiguration.Shutters[4].Should().Be("Mix 1.2-3_4");
	}

	[Fact]
	public void MultipleLoadCalls_OverwritesPreviousConfiguration()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareValidCase("Baseline");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		service.Load(filePath);
		service.CurrentConfiguration.Shutters[0].Should().Be("Shutter1");

		var newDto = service.CreateEmptyConfiguration();
		newDto.Shutters[0] = "UpdatedShutter";
		service.Save(filePath, newDto);

		service.Load(filePath);

		service.CurrentConfiguration.Shutters[0].Should().Be("UpdatedShutter");
	}

	private static LoaderDto CreateTestDto()
	{
		var shutters = CreateEmptyArray(16);
		shutters[0] = "MyShutter";
		shutters[1] = "Another Shutter";

		var sources = CreateEmptyArray(32);
		sources[0] = "Source-1";

		var heaters = CreateEmptyArray(16);
		heaters[0] = "Heater.1";

		var water = CreateEmptyArray(16);
		water[0] = "Water_Pump";

		return new LoaderDto(shutters, sources, heaters, water);
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
}
