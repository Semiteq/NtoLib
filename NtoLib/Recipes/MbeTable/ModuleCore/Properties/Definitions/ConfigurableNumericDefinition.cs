using System;
using System.Globalization;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

/// <summary>
/// Configurable numeric definition supporting ranges and formatting strategies.
/// Works with int16 and float at runtime (values are stored as float or int16).
/// </summary>
public class ConfigurableNumericDefinition : IPropertyTypeDefinition
{
    private readonly Type _systemType;
    private readonly float? _min;
    private readonly float? _max;
    private readonly FormatKind _formatKind;
    public FormatKind FormatKind => _formatKind;

    private const int Precision = 2;

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
        _formatKind = Enum.TryParse<FormatKind>(dto.FormatKind, ignoreCase: true, out var parsed)
            ? parsed
            : FormatKind.Numeric;
    }

    /// <inheritdoc/>
    public virtual Result TryValidate(object value)
    {
        float? numeric = value switch
        {
            short i => i,
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
        float numeric = value switch
        {
            short i => i,
            float f => f,
            _ => float.NaN
        };

        if (float.IsNaN(numeric))
            return value.ToString() ?? string.Empty;

        return _formatKind switch
        {
            FormatKind.Scientific => numeric.ToString("0.##E0", CultureInfo.InvariantCulture),
            FormatKind.Numeric => numeric.ToString("0.##", CultureInfo.InvariantCulture),
            _ => numeric.ToString(CultureInfo.InvariantCulture)
        };
    }

    /// <inheritdoc/>
    public virtual Result<object> TryParse(string input)
    {
        var sanitized = new string(input.Trim()
                .Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == 'E' || c == 'e' || c == '+' || c == '-' || c == ':')
                .ToArray())
            .Replace(',', '.');

        if (_systemType == typeof(short))
        {
            if (short.TryParse(sanitized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return Result.Ok<object>(i);

            if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out var fi))
                return Result.Ok<object>((short)fi);

            return Result.Fail<object>("Unable to parse as Int16");
        }

        if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
        {
            f = (float)Math.Round(f, Precision, MidpointRounding.AwayFromZero);
            return Result.Ok<object>(f);
        }

        return Result.Fail<object>("Unable to parse as float");
    }
}