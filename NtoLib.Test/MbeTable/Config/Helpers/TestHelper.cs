using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

namespace NtoLib.Test.MbeTable.Config.Helpers;

public static class TestHelper
{
    private const string PropertyFile = "PropertyDefs.yaml";
    private const string ColumnFile = "ColumnDefs.yaml";
    private const string PinGroupFile = "PinGroupDefs.yaml";
    private const string ActionFile = "ActionsDefs.yaml";

    public static AppConfiguration LoadValidCase(string caseName)
    {
        using var tempDir = TestDataCopier.PrepareValidCase(caseName);
        var loader = new ConfigurationLoader();
        return loader.LoadConfiguration(tempDir.Path, PropertyFile, ColumnFile, PinGroupFile, ActionFile);
    }

    public static ConfigException LoadInvalidCaseExpectingError(
        string caseName,
        string expectedSection,
        string expectedContextContains)
    {
        using var tempDir = TestDataCopier.PrepareInvalidCase(caseName);
        var loader = new ConfigurationLoader();

        Action act = () => loader.LoadConfiguration(
            tempDir.Path,
            PropertyFile,
            ColumnFile,
            PinGroupFile,
            ActionFile);

        var ex = act.Should().Throw<ConfigException>().Which;
        ex.ShouldContainError(expectedSection, expectedContextContains);
        return ex;
    }

    public static ConfigException LoadInvalidCaseExpectingAnyError(string caseName)
    {
        using var tempDir = TestDataCopier.PrepareInvalidCase(caseName);
        var loader = new ConfigurationLoader();

        Action act = () => loader.LoadConfiguration(
            tempDir.Path,
            PropertyFile,
            ColumnFile,
            PinGroupFile,
            ActionFile);

        return act.Should().Throw<ConfigException>().Which;
    }
}