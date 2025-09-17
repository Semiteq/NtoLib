#nullable enable

using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

/// <summary>
/// Configurable boolean definition.
/// </summary>
public sealed class ConfigurableBooleanDefinition : IPropertyTypeDefinition
{
    /// <inheritdoc/>
    public string Units => string.Empty;

    /// <inheritdoc/>
    public Type SystemType => typeof(bool);

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ConfigurableBooleanDefinition(YamlPropertyDefinition _)
    {
    }

    /// <inheritdoc/>
    public Result TryValidate(object value)
    {
        return value is bool 
            ? Result.Ok() 
            : Result.Fail("Value must be a boolean");
    }

    /// <inheritdoc/>
    public string FormatValue(object value) => value?.ToString() ?? string.Empty;

    /// <inheritdoc/>
    public Result<object> TryParse(string input)
    {
        if (bool.TryParse(input, out var b)) 
            return Result.Ok<object>(b);
        
        if (int.TryParse(input, out var i)) 
            return Result.Ok<object>(i != 0);
        
        return Result.Fail<object>("Unable to parse as boolean");
    }
}