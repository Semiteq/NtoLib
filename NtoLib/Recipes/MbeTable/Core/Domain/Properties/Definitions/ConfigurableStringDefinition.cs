#nullable enable
using System;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

/// <summary>
/// Configurable string definition with optional MaxLength constraint.
/// </summary>
public sealed class ConfigurableStringDefinition : IPropertyTypeDefinition
{
    private readonly int _maxLength;

    /// <inheritdoc/>
    public string Units => string.Empty;

    /// <inheritdoc/>
    public Type SystemType => typeof(string);

    /// <summary>
    /// Initializes a new instance from a DTO.
    /// </summary>
    public ConfigurableStringDefinition(YamlPropertyDefinition dto)
    {
        _maxLength = Math.Max(0, dto.MaxLength ?? 255);
    }

    /// <inheritdoc/>
    public Result TryValidate(object value)
    {
        var s = value?.ToString() ?? string.Empty;
        return s.Length > _maxLength
            ? Result.Fail($"String length must be <= {_maxLength}")
            : Result.Ok();
    }

    /// <inheritdoc/>
    public string FormatValue(object value) => value?.ToString() ?? string.Empty;

    /// <inheritdoc/>
    public Result<object> TryParse(string input) => Result.Ok<object>(input ?? string.Empty);
}