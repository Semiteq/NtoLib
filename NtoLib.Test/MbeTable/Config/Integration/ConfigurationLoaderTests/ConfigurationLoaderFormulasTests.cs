using FluentAssertions;

using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

public sealed class ConfigurationLoaderFormulasTests
{
    [Fact]
    public void EmptyExpression_FailsAtFormulaValidator()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "FormulasEmptyExpression",
            "ActionsDefs.yaml",
            "ActionId=3000");
    }

    [Fact]
    public void RecalcOrderWithDuplicates_FailsInFormulaDefsValidator()
    {
        TestHelper.LoadInvalidCaseExpectingError(
            "FormulasDuplicateRecalcOrder",
            "ActionsDefs.yaml",
            "ActionId=3001");
    }

    [Fact]
    public void RecalcOrderReferencesMissingColumn_Fails()
    {
        var ex = TestHelper.LoadInvalidCaseExpectingAnyError("FormulasMissingVariable");

        ex.Errors.Should().ContainSingle(e =>
            e.Section == "ActionsDefs.yaml" &&
            e.Message.Contains("Formula references missing columns") &&
            e.Context.Contains("ActionId=3002"));
    }
}