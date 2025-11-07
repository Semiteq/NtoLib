using FluentAssertions;

using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "YamlParsing")]
public sealed class ConfigurationLoaderYamlParsingTests
{
    [Fact]
    public void ActionsSyntaxError_Fails()
    {
        var ex = TestHelper.LoadInvalidCaseExpectingAnyError("YamlSyntaxErrorActions");

        ex.Errors.Should().Contain(e =>
            e.Section == "YAML" &&
            e.Context.Contains("deserialization") &&
            e.Message.Contains("Failed to deserialize YAML"));
    }
}