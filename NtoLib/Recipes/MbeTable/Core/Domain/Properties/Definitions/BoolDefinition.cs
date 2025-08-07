using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class BoolDefinition : IPropertyTypeDefinition
{
    public string Units => "";
    public Type SystemType => typeof(bool);

    public bool Validate(object value, out string errorMessage)
    {
        errorMessage = string.Empty;
        return value is bool; // Всегда валидно, если тип правильный
    }
        
    public string FormatValue(object value) => value.ToString();

    public bool TryParse(string input, out object value)
    {
        if (bool.TryParse(input, out var result))
        {
            value = result;
            return true;
        }
        value = default(bool);
        return false;
    }
}