using System;
using System.Globalization;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class TemperatureDefinition : IPropertyTypeDefinition
{
    public string Units => "°C";
    public Type SystemType => typeof(float);

    public (bool Success, string errorMessage) Validate(object value)
    {
        if ((float)value is < 0 or > 2000)
        {
            return (false, "Температура должна быть в пределах от 0 до 2000");
        }
        return (true, "");
    }

    public virtual string FormatValue(object value)
    {
        var floatValue = (float)value;
        return floatValue % 1 == 0
            ? floatValue.ToString("F0", CultureInfo.InvariantCulture)
            : floatValue.ToString("G", CultureInfo.InvariantCulture);
    }

    public (bool Success, object Value) TryParse(string input)
    {
        // Drop all non-chars and replace "," with "."
        var sanitizedInput = new string(input.Trim().Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
            .Replace(',', '.');

        if (float.TryParse(sanitizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return (true, result);

        return (false, 0f);
    }
}