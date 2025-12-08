using FluentAssertions;

using Tests.ConfigLoader.Helpers;

using Xunit;

namespace Tests.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Load")]
public sealed class LoadTests
{
	[Fact]
	public void Load_ValidFile_ReturnsSuccess()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareValidCase("Baseline");
		using var _ = tempDir;

		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Load(filePath);

		result.IsSuccess.Should().BeTrue();
		service.IsLoaded.Should().BeTrue();
		service.LastError.Should().BeEmpty();
		service.CurrentConfiguration.Shutters[0].Should().Be("Shutter1");
		service.CurrentConfiguration.Shutters[1].Should().Be("Shutter2");
		service.CurrentConfiguration.Sources[0].Should().Be("Source1");
	}

	[Fact]
	public void Load_FileNotExists_CreatesDefaultAndReturnsSuccess()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();

		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Load(filePath);

		result.IsSuccess.Should().BeTrue();
		service.IsLoaded.Should().BeTrue();
		File.Exists(filePath).Should().BeTrue();
		service.CurrentConfiguration.Shutters.Should().AllBe(string.Empty);
	}

	[Fact]
	public void Load_MissingGroup_ReturnsError()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareInvalidCase("MissingGroup");
		using var _ = tempDir;

		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Load(filePath);

		result.IsFailed.Should().BeTrue();
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().Contain("ChamberHeater");
		service.LastError.Should().Contain("missing");
	}

	[Fact]
	public void Load_MissingKey_ReturnsError()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareInvalidCase("MissingKey");
		using var _ = tempDir;

		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Load(filePath);

		result.IsFailed.Should().BeTrue();
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().Contain("missing key");
	}

	[Fact]
	public void Load_InvalidCharacters_ReturnsError()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareInvalidCase("InvalidCharacters");
		using var _ = tempDir;

		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var result = service.Load(filePath);

		result.IsFailed.Should().BeTrue();
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().Contain("invalid characters");
	}

	[Fact]
	public void Load_EmptyPath_ReturnsError()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		var result = service.Load("");

		result.IsFailed.Should().BeTrue();
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().Contain("empty");
	}
}
