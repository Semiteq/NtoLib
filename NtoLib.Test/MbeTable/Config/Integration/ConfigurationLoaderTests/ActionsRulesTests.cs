using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Actions")]
public sealed class ActionsRulesTests
{
	[Fact]
	public void LongLastingWithoutStepDuration_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"ActionsLongLastingWithoutStepDuration",
			"ActionsDefs.yaml",
			"ActionId=10");
	}

	[Fact]
	public void EnumWithoutGroupName_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"ActionsEnumWithoutGroupName",
			"ActionsDefs.yaml",
			"ColumnKey='target'");
	}

	[Fact]
	public void InvalidDeployDuration_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"ActionsInvalidDeployDuration",
			"ActionsDefs.yaml",
			"ActionId=1100");
	}

	[Fact]
	public void DuplicateActionId_Fails()
	{
		TestHelper.LoadInvalidCaseExpectingError(
			"ActionsDuplicateId",
			"ActionsDefs.yaml",
			"ActionId=1100");
	}
}
