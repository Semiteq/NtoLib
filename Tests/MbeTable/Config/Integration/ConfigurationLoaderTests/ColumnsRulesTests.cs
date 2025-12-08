using FluentAssertions;

using Tests.MbeTable.Config.Helpers;

using Xunit;

namespace Tests.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Columns")]
public sealed class ColumnsRulesTests
{
	[Fact]
	public void MissingTask_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsMissingTask");

		ex.Errors.Should()
			.Contain(e => e.Section == "ColumnDefs.yaml" && e.Message.Contains("Missing mandatory columns"));
	}

	[Fact]
	public void WidthMinusOneOnNonComment_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsWidthMinusOneOnNonComment");

		ex.Errors.Should()
			.Contain(e => e.Section == "ColumnDefs.yaml" && e.Message.Contains("Width=-1 is only allowed"));
	}

	[Fact]
	public void UnsupportedColumnType_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsUnsupportedType");

		ex.Errors.Should()
			.Contain(e => e.Section == "ColumnDefs.yaml" && e.Message.Contains("Unsupported column types"));
	}

	[Fact]
	public void ActionComboBindingNotOnAction_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsInvalidActionComboBinding");

		ex.Errors.Should().Contain(e =>
			e.Section == "ColumnDefs.yaml" && e.Message.Contains("can only be used with key='action'"));
	}

	[Fact]
	public void MaxDropdownItemsZero_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsInvalidMaxDropdown");

		ex.Errors.Should().Contain(e =>
			e.Section == "ColumnDefs.yaml" && e.Message.Contains("max_dropdown_items must be > 0"));
	}

	[Fact]
	public void NegativePlcIndex_Fails()
	{
		var ex = TestHelper.LoadInvalidCaseExpectingAnyError("ColumnsNegativePlcIndex");

		ex.Errors.Should().Contain(e => e.Section == "ColumnDefs.yaml" && e.Message.Contains("must be >= 0"));
	}
}
