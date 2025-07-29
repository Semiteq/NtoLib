using System;
using System.Globalization;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Contracts;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions;

public class TemperatureDefinition : IPropertyTypeDefinition
{
    public string Units => "°C";
    public Type SystemType => typeof(float);

    public bool Validate(object value, out string errorMessage)
    {
        errorMessage = string.Empty;
        var floatValue = (float)value;
        if (floatValue is < 0 or > 2000)
        {
            errorMessage = $"Температура должна быть в пределах от 0 до 2000";
            return false;
        }
        return true;
    }

    public string FormatValue(object value) => ((float)value).ToString(CultureInfo.InvariantCulture);

    public bool TryParse(string input, out object value)
    {
        if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            value = result;
            return true;
        }
        value = default;
        return false;
    }
}