using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;
using NtoLib.Test.MbeTable.Config.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.FormulaPrecompilerTests;

[Trait("Category", "Integration")]
[Trait("Component", "FormulaPrecompiler")]
[Trait("Area", "Formulas")]
public sealed class FormulaPrecompilerTests
{
    [Fact]
    public void Precompile_ValidLinearFormula_Succeeds()
    {
        var config = TestHelper.LoadValidCase("WithValidFormula");

        var logger = new NullLogger<FormulaPrecompiler>();
        var precompiler = new FormulaPrecompiler(logger);

        var result = precompiler.Precompile(config.Actions);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey(4000);
        result.Value[4000].Should().NotBeNull();
    }

    [Fact]
    public void Precompile_SyntaxErrorInExpression_FailsWithCompilationError()
    {
        var ex = TestHelper.LoadInvalidCaseExpectingAnyError("FormulaCompileSyntaxError");

        var error = ex.Errors.Should().ContainSingle(e =>
            e.Section == "ActionsDefs.yaml" &&
            e.Message.Contains("Failed to compile formula") &&
            e.Context.Contains("ActionId=5001")).Subject;

        error.Metadata.Should().ContainKey("actionId");
        error.Metadata["actionId"].Should().Be("5001");
    }

    [Fact]
    public void Precompile_MultipleActionsWithFormulas_CompilesAll()
    {
        var config = TestHelper.LoadValidCase("WithMultipleFormulas");

        var logger = new NullLogger<FormulaPrecompiler>();
        var precompiler = new FormulaPrecompiler(logger);

        var result = precompiler.Precompile(config.Actions);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKeys(4000, 4001);
    }
}