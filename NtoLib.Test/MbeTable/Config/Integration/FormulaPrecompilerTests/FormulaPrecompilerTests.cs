using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

using Xunit;

namespace NtoLib.Test.MbeTable.Config.Integration.FormulaPrecompilerTests;

public sealed class FormulaPrecompilerTests
{
    [Fact]
    public void Precompile_ValidFormula_Succeeds()
    {
        var action = new ActionDefinition(
            Id: 1,
            Name: "A",
            Columns: new[]
            {
                new PropertyConfig { Key = "x", PropertyTypeId = "float" },
                new PropertyConfig { Key = "y", PropertyTypeId = "float" }
            },
            DeployDuration: NtoLib.Recipes.MbeTable.ModuleCore.Entities.DeployDuration.Immediate,
            Formula: new FormulaDefinition
            {
                Expression = "x + y",
                RecalcOrder = new[] { "x", "y" }
            });

        var logger = new NullLogger<FormulaPrecompiler>();
        var precompiler = new FormulaPrecompiler(logger);

        var result = precompiler.Precompile(new Dictionary<short, ActionDefinition> { { 1, action } });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey(1);
    }
}