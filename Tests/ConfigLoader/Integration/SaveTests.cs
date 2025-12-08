using FluentAssertions;

using NtoLib.ConfigLoader.Entities;

using Tests.ConfigLoader.Helpers;

using Xunit;

namespace Tests.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Save")]
public sealed class SaveTests
{
	[Fact]
	public void Save_ValidDto_CreatesFile()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var dto = service.CreateEmptyConfiguration();

		var result = service.Save(filePath, dto);

		result.IsSuccess.Should().BeTrue();
		File.Exists(filePath).Should().BeTrue();
	}

	[Fact]
	public void Save_DirectoryNotExists_CreatesDirectory()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = Path.Combine(tempDir.Path, "SubDir", "NamesConfig.yaml");

		var dto = service.CreateEmptyConfiguration();

		var result = service.Save(filePath, dto);

		result.IsSuccess.Should().BeTrue();
		File.Exists(filePath).Should().BeTrue();
	}

	[Fact]
	public void Save_NullDto_ReturnsError()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Save(filePath, null);

		result.IsFailed.Should().BeTrue();
		service.LastError.Should().Contain("null");
	}

	[Fact]
	public void Save_EmptyPath_ReturnsError()
	{
		var service = ConfigLoaderTestHelper.CreateService();
		var dto = service.CreateEmptyConfiguration();

		var result = service.Save("", dto);

		result.IsFailed.Should().BeTrue();
		service.LastError.Should().Contain("empty");
	}

	[Fact]
	public void Save_WritesDoubleQuotedValues()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var dto = new LoaderDto(
			new[] { "TestShutter", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" },
			new string[32],
			new string[16],
			new string[16]);

		for (var i = 0; i < dto.Sources.Length; i++)
			dto.Sources[i] = "";
		for (var i = 0; i < dto.ChamberHeaters.Length; i++)
			dto.ChamberHeaters[i] = "";
		for (var i = 0; i < dto.WaterChannels.Length; i++)
			dto.WaterChannels[i] = "";

		var result = service.Save(filePath, dto);

		result.IsSuccess.Should().BeTrue();

		var content = File.ReadAllText(filePath);
		content.Should().Contain("\"TestShutter\"");
		content.Should().Contain("\"\"");
	}
}
