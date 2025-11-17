using System.Linq;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NtoLib.Recipes.MbeTable.ModuleConfig;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

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
        var config = loader.LoadConfiguration(tempDir.Path, PropertyFile, ColumnFile, PinGroupFile, ActionFile);

        EnsurePrecompiledOrThrow(config);
        return config;
    }

    public static ConfigException LoadInvalidCaseExpectingError(
        string caseName,
        string expectedSection,
        string expectedContextContains)
    {
        using var tempDir = TestDataCopier.PrepareInvalidCase(caseName);
        var loader = new ConfigurationLoader();

        try
        {
            var config = loader.LoadConfiguration(tempDir.Path, PropertyFile, ColumnFile, PinGroupFile, ActionFile);

            var ex = TryPrecompileAndConvert(config);
            ex.Should().NotBeNull("Expected precompile to fail for invalid case.");

            ex!.ShouldContainError(expectedSection, expectedContextContains);
            return ex;
        }
        catch (ConfigException ex)
        {
            ex.ShouldContainError(expectedSection, expectedContextContains);
            return ex;
        }
    }

    public static ConfigException LoadInvalidCaseExpectingAnyError(string caseName)
    {
        using var tempDir = TestDataCopier.PrepareInvalidCase(caseName);
        var loader = new ConfigurationLoader();

        try
        {
            var config = loader.LoadConfiguration(tempDir.Path, PropertyFile, ColumnFile, PinGroupFile, ActionFile);

            var ex = TryPrecompileAndConvert(config);
            if (ex != null)
            {
                return ex;
            }

            throw new Xunit.Sdk.XunitException(
                "Expected a ConfigException to be thrown, but neither load nor precompile failed.");
        }
        catch (ConfigException ex)
        {
            return ex;
        }
    }

    private static void EnsurePrecompiledOrThrow(AppConfiguration config)
    {
        var precompiler = new FormulaPrecompiler(NullLogger<FormulaPrecompiler>.Instance);
        var precompileResult = precompiler.Precompile(config.Actions);

        if (precompileResult.IsFailed)
        {
            var configErrors = precompileResult.Errors
                .Select(e => e as ConfigError ?? new ConfigError(e.Message, "ActionsDefs.yaml", "formula-precompile"))
                .ToList();

            throw new ConfigException(configErrors);
        }
    }

    private static ConfigException? TryPrecompileAndConvert(AppConfiguration config)
    {
        var precompiler = new FormulaPrecompiler(NullLogger<FormulaPrecompiler>.Instance);
        var precompileResult = precompiler.Precompile(config.Actions);

        if (precompileResult.IsSuccess)
            return null;

        var configErrors = precompileResult.Errors
            .Select(e => e as ConfigError ?? new ConfigError(e.Message, "ActionsDefs.yaml", "formula-precompile"))
            .ToList();

        return new ConfigException(configErrors);
    }
}