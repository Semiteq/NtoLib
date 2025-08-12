using System;
using System.Globalization;
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

    public string FormatValue(object value) => ((float)value).ToString(CultureInfo.InvariantCulture);

    public (bool Success, object Value) TryParse(string input)
    {
        if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return (true, result);
        
        return (false, 0f);
    }
}