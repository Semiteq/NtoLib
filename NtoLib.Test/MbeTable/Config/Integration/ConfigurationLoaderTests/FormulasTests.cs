using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.ConfigurationLoaderTests;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Formulas")]
public sealed class FormulasTests
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
        TestHelper.LoadInvalidCaseExpectingError(
            "FormulasMissingVariable",
            "ActionsDefs.yaml",
            "ActionId=3002");
    }
}