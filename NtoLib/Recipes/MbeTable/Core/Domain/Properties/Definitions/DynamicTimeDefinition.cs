#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Properties;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

/// <summary>
/// Time definition with hh:mm:ss parsing/formatting, bounds, and seconds backing store.
/// </summary>
public sealed class DynamicTimeDefinition : ConfigurableNumericDefinition
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public DynamicTimeDefinition(YamlPropertyDefinition dto) : base(dto)
    {
    }

    /// <inheritdoc/>
    public override string FormatValue(object value)
    {
        var seconds = value switch
        {
            float f => f,
            int i => i,
            _ => 0f
        };
        var t = TimeSpan.FromSeconds(seconds);
        return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds + t.Milliseconds / 1000.0:00.###}";
    }

    /// <inheritdoc/>
    public override Result<object> TryParse(string input)
    {
        var sanitized = new string(input
            .Where(c => char.IsDigit(c) || c == ',' || c == ':' || c == '.')
            .ToArray()).Replace(',', '.');

        // Parse format hh:mm:ss or hh:mm:ss.ms
        if (sanitized.Contains(":"))
        {
            var regex = new Regex(@"^(?<h>\d{1,2}):(?<m>\d{1,2})(:(?<s>\d{1,2})(\.(?<ms>\d+))?)?$");
            var match = regex.Match(sanitized);
            if (!match.Success)
                return Result.Fail<object>("Invalid time format");

            if (!int.TryParse(match.Groups["h"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
                return Result.Fail<object>("Invalid hours value");
            if (!int.TryParse(match.Groups["m"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m))
                return Result.Fail<object>("Invalid minutes value");

            var s = 0;
            var ms = 0f;
            if (match.Groups["s"].Success && !int.TryParse(match.Groups["s"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out s))
                return Result.Fail<object>("Invalid seconds value");
            if (match.Groups["ms"].Success && !float.TryParse($"0.{match.Groups["ms"].Value}", NumberStyles.Float, CultureInfo.InvariantCulture, out ms))
                return Result.Fail<object>("Invalid milliseconds value");

            var total = h * 3600 + m * 60 + s + ms;
            return Result.Ok<object>(total);
        }

        // Usual numeric parsing processing as float in base 
        return base.TryParse(input);
    }
}