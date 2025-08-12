using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class StringDefinition : IPropertyTypeDefinition
{
    public string Units => "";
    public Type SystemType => typeof(string);

    public (bool Success, string errorMessage) Validate(object value)
    {
        if (value.ToString().Length is < 0 or > 255)
            return (false, "Длина строки должна быть от 0 до 255 символов");
        
        return (true, "");
    }
    
    public string FormatValue(object value) => (string)value;

    public (bool Success, object Value) TryParse(string input)
    {
        return (true, "");
    }
}