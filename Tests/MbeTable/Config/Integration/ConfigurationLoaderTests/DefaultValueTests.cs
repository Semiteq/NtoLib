using FluentAssertions;

using Tests.MbeTable.Config.Helpers;

using Xunit;

namespace Tests.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "DefaultValue")]
public sealed class DefaultValueTests
{
	[Fact]
	public void StringDefaultValue_ExceedsMaxLength_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"DefaultValueStringTooLong",
			"ActionsDefs.yaml",
			"ColumnKey='comment'");
	}

	[Fact]
	public void Int16DefaultValue_NotParsable_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"DefaultValueInt16NotParsable",
			"ActionsDefs.yaml",
			"ColumnKey='int_value'");
	}

	[Fact]
	public void FloatDefaultValue_OutOfRange_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"DefaultValueFloatOutOfRange",
			"ActionsDefs.yaml",
			"ColumnKey='speed'");
	}

	[Fact]
	public void DefaultValue_OnReadOnlyColumn_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"DefaultValueReadOnlyConflict",
			"ActionsDefs.yaml",
			"ColumnKey='step_start_time'");
	}

	[Fact]
	public void FloatDefaultValue_ParsedWithInvariantCulture_UnderRuRu_Succeeds()
	{
		using var culture = new CultureScope("ru-RU");

		var config = TestHelper.LoadValidCase("Baseline");

		config.Actions.Should().ContainKey(10);
		config.Actions.Should().ContainKey(1100);
	}
}
