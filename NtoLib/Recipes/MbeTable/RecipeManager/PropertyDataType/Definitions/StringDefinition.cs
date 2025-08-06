using System;
using NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Contracts;

namespace NtoLib.Recipes.MbeTable.RecipeManager.PropertyDataType.Definitions;

public class StringDefinition : IPropertyTypeDefinition
{
    public string Units => "";
    public Type SystemType => typeof(string);

    public bool Validate(object value, out string errorMessage)
    {
        errorMessage = string.Empty;
        var stringValue = (string)value;
        if (stringValue.Length is < 0 or > 255)
        {
            errorMessage = $"Длина строки должна быть от 0 до 255 символов";
            return false;
        }
        return true;
    }
    
    public string FormatValue(object value) => (string)value;

    public bool TryParse(string input, out object value)
    {
        value = input ?? string.Empty;
        return true; // Строка всегда может быть "распарсена"
    }
}