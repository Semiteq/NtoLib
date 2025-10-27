using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

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

    public FormatKind FormatKind => FormatKind.Numeric;

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
            ? Result.Fail(
                new Error($"String length must be <= {_maxLength}").WithMetadata(nameof(Codes),
                    Codes.PropertyValidationFailed))
            : Result.Ok();
    }

    /// <inheritdoc/>
    public string FormatValue(object value) => value?.ToString() ?? string.Empty;

    /// <inheritdoc/>
    public Result<object> TryParse(string input) => Result.Ok<object>(input ?? string.Empty);
}