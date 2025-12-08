using FluentAssertions;

using Tests.ConfigLoader.Helpers;

using Xunit;

namespace Tests.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "ServiceState")]
public sealed class ServiceStateTests
{
	[Fact]
	public void NewService_IsLoadedIsFalse()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		service.IsLoaded.Should().BeFalse();
	}

	[Fact]
	public void NewService_LastErrorIsEmpty()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		service.LastError.Should().BeEmpty();
	}

	[Fact]
	public void NewService_CurrentConfigurationIsEmpty()
	{
		var service = ConfigLoaderTestHelper.CreateService();

		service.CurrentConfiguration.Shutters.Should().AllBe(string.Empty);
		service.CurrentConfiguration.Sources.Should().AllBe(string.Empty);
		service.CurrentConfiguration.ChamberHeaters.Should().AllBe(string.Empty);
		service.CurrentConfiguration.WaterChannels.Should().AllBe(string.Empty);
	}

	[Fact]
	public void AfterSuccessfulLoad_IsLoadedIsTrue()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareValidCase("Baseline");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		service.Load(filePath);

		service.IsLoaded.Should().BeTrue();
	}

	[Fact]
	public void AfterSuccessfulLoad_LastErrorIsEmpty()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareValidCase("Baseline");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		service.Load(filePath);

		service.LastError.Should().BeEmpty();
	}

	[Fact]
	public void AfterFailedLoad_IsLoadedIsFalse()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareInvalidCase("MissingGroup");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		service.Load(filePath);

		service.IsLoaded.Should().BeFalse();
	}

	[Fact]
	public void AfterFailedLoad_LastErrorContainsMessage()
	{
		var (service, tempDir) = ConfigLoaderTestHelper.PrepareInvalidCase("MissingGroup");
		using var _ = tempDir;
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		service.Load(filePath);

		service.LastError.Should().NotBeEmpty();
	}

	[Fact]
	public void SuccessfulLoadAfterFailedLoad_ClearsError()
	{
		using var invalidTempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		using var validTempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();

		var invalidPath = ConfigLoaderTestHelper.GetConfigFilePath(invalidTempDir);
		System.IO.File.WriteAllText(invalidPath, "invalid yaml content {{{{");
		service.Load(invalidPath);
		service.IsLoaded.Should().BeFalse();
		service.LastError.Should().NotBeEmpty();

		var validPath = ConfigLoaderTestHelper.GetConfigFilePath(validTempDir);
		service.Load(validPath);

		service.IsLoaded.Should().BeTrue();
		service.LastError.Should().BeEmpty();
	}
}
