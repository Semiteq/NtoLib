#nullable enable
using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

/// <summary>
/// Integer-based enum identifier (for targets etc.).
/// </summary>
public sealed class ConfigurableEnumDefinition : IPropertyTypeDefinition
{
    /// <inheritdoc/>
    public string Units => string.Empty;

    /// <inheritdoc/>
    public Type SystemType => typeof(int);

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ConfigurableEnumDefinition(YamlPropertyDefinition _)
    {
    }

    /// <inheritdoc/>
    public Result TryValidate(object value)
    {
        return value is int
            ? Result.Ok()
            : Result.Fail("Value must be an integer");
    }

    /// <inheritdoc/>
    public string FormatValue(object value) => value.ToString() ?? string.Empty;

    /// <inheritdoc/>
    public Result<object> TryParse(string input)
    {
        return int.TryParse(input, out var v)
            ? Result.Ok<object>(v)
            : Result.Fail<object>("Unable to parse as integer");
    }
}