using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "FileSystem")]
public sealed class FileSystemTests
{
    [Fact]
    public void LoadConfiguration_DirectoryMissing_ThrowsConfigException()
    {
        var loader = new ConfigurationLoader();
        Action act = () => loader.LoadConfiguration(
            @"Z:\non-existent\path\for\ntolib\tests",
            "PropertyDefs.yaml",
            "ColumnDefs.yaml",
            "PinGroupDefs.yaml",
            "ActionsDefs.yaml");

        act.Should().Throw<ConfigException>()
            .Which.ShouldContainError("filesystem", "directory-check");
    }

    [Fact]
    public void LoadConfiguration_MissingFiles_ThrowsWithListOfMissing()
    {
        using var dir = new TempDirectory();

        var loader = new ConfigurationLoader();
        Action act = () => loader.LoadConfiguration(
            dir.Path,
            "PropertyDefs.yaml",
            "ColumnDefs.yaml",
            "PinGroupDefs.yaml",
            "ActionsDefs.yaml");

        var ex = act.Should().Throw<ConfigException>().Which;
        var err = ex.ShouldContainError("filesystem", "files-check");
        err.Message.Should().Contain("Missing configuration files");
    }
}