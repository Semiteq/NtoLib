using System;
using System.Globalization;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

/// <summary>
/// Integer-based enum identifier (ActionId, TargetId, etc.), stored strictly as <see cref="short"/>.
/// </summary>
public sealed class ConfigurableEnumDefinition : IPropertyTypeDefinition
{
    public string Units { get; }
    public Type SystemType => typeof(short);

    public FormatKind FormatKind => FormatKind.Numeric;
    
    public ConfigurableEnumDefinition(YamlPropertyDefinition dto)
    {
        Units = dto.Units ?? string.Empty;
    }

    public Result TryValidate(object value)
        => value is short
            ? Result.Ok()
            : Result.Fail("Value must be Int16");

    public string FormatValue(object value) => value.ToString() ?? string.Empty;

    public Result<object> TryParse(string input)
    {
        var style = NumberStyles.Integer;
        var ic    = CultureInfo.InvariantCulture;

        return short.TryParse(input, style, ic, out var s)
            ? Result.Ok<object>(s)
            : Result.Fail<object>($"Unable to parse '{input}' as Int16.");
    }
}