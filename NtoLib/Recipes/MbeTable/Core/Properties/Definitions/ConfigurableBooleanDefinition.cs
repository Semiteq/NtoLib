using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Dto.Properties;
using NtoLib.Recipes.MbeTable.Core.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Properties.Definitions;

/// <summary>
/// Boolean property type driven by YAML definition.
/// Any non-zero numeric string is treated as <c>true</c>.
/// </summary>
public sealed class ConfigurableBooleanDefinition : IPropertyTypeDefinition
{
    public string Units { get; }

    public Type SystemType => typeof(bool);

    public FormatKind FormatKind => FormatKind.Numeric;
    
    public ConfigurableBooleanDefinition(YamlPropertyDefinition dto)
    {
        Units = dto.Units ?? string.Empty;
    }

    public Result TryValidate(object value)
        => value is bool
            ? Result.Ok()
            : Result.Fail("Value must be a boolean");

    public string FormatValue(object value) => (value as bool?)?.ToString() ?? string.Empty;

    public Result<object> TryParse(string input)
    {
        if (bool.TryParse(input, out var b))
            return Result.Ok<object>(b);

        if (short.TryParse(input, out var i))
            return Result.Ok<object>(i != 0);

        return Result.Fail<object>($"Unable to parse '{input}' as boolean.");
    }
}