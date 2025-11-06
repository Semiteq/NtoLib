using FluentAssertions;

using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

public sealed class ConfigurationLoaderCrossReferenceTests
{
    [Fact]
    public void ActionReferencesMissingColumn_Fails()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "CrossActionMissingColumn",
            "ActionsDefs.yaml",
            "ColumnKey=");
    }

    [Fact]
    public void ActionReferencesMissingPropertyType_Fails()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "CrossActionMissingPropertyType",
            "ActionsDefs.yaml",
            "ColumnKey=");
    }

    [Fact]
    public void ColumnReferencesMissingPropertyType_Fails()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "CrossColumnMissingPropertyType",
            "ColumnDefs.yaml",
            "Key=");
    }

    [Fact]
    public void EnumGroupNameNotDefined_Fails()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "CrossEnumGroupMissing",
            "ActionsDefs.yaml",
            "ColumnKey='target'");
    }
}