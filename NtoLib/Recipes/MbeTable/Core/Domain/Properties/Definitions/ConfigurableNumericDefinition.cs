#nullable enable
using System;
using System.Globalization;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

/// <summary>
/// Configurable numeric definition supporting ranges and formatting strategies.
/// Works with int and float at runtime (values are stored as float or int).
/// </summary>
public class ConfigurableNumericDefinition : IPropertyTypeDefinition
{
    private readonly Type _systemType;
    private readonly float? _min;
    private readonly float? _max;
    private readonly string _formatKind;

    /// <inheritdoc/>
    public string Units { get; }

    /// <inheritdoc/>
    public Type SystemType => _systemType;

    /// <summary>
    /// Initializes a new instance from a DTO.
    /// </summary>
    public ConfigurableNumericDefinition(YamlPropertyDefinition dto)
    {
        _systemType = Type.GetType(dto.SystemType, throwOnError: true, ignoreCase: true)!;
        Units = dto.Units ?? string.Empty;
        _min = dto.Min;
        _max = dto.Max;
        _formatKind = dto.FormatKind ?? "Auto";
    }

    /// <inheritdoc/>
    public virtual Result TryValidate(object value)
    {
        float? numeric = value switch
        {
            int i => i,
            float f => f,
            double d => (float)d,
            _ => null
        };

        if (!numeric.HasValue)
            return Result.Fail($"Expected numeric value, got {value.GetType().Name}");

        if (_min.HasValue && numeric.Value < _min.Value)
            return Result.Fail($"Value must be >= {_min.Value}");

        if (_max.HasValue && numeric.Value > _max.Value)
            return Result.Fail($"Value must be <= {_max.Value}");

        return Result.Ok();
    }

    /// <inheritdoc/>
    public virtual string FormatValue(object value)
    {
        var formatResult = TryFormatInternal(value);
        return formatResult.IsSuccess ? formatResult.Value.display : value.ToString() ?? string.Empty;
    }

    /// <inheritdoc/>
    public virtual Result<object> TryParse(string input)
    {
        var sanitized = new string(input.Trim()
                .Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == 'E' || c == 'e' || c == '+' || c == '-' || c == ':')
                .ToArray())
            .Replace(',', '.');

        if (_systemType == typeof(int))
        {
            if (int.TryParse(sanitized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return Result.Ok<object>(i);

            if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out var fi))
                return Result.Ok<object>((int)fi);

            return Result.Fail<object>("Unable to parse as integer");
        }

        if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            return Result.Ok<object>(f);

        return Result.Fail<object>("Unable to parse as float");
    }

    private Result<(float numeric, string display)> TryFormatInternal(object value)
    {
        float numeric = value switch
        {
            int i => i,
            float f => f,
            double d => (float)d,
            _ => float.NaN
        };
        
        if (float.IsNaN(numeric))
            return Result.Fail<(float numeric, string display)>("Invalid numeric value");

        var display = _formatKind switch
        {
            "Scientific" => numeric.ToString("0.##E0", CultureInfo.InvariantCulture),
            "Auto" => numeric % 1 == 0
                ? numeric.ToString("F0", CultureInfo.InvariantCulture)
                : numeric.ToString("G2", CultureInfo.InvariantCulture),
            _ => numeric.ToString(CultureInfo.InvariantCulture)
        };

        return Result.Ok((numeric, display));
    }
}