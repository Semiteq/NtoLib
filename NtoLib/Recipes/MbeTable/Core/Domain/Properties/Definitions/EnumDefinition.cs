using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class EnumDefinition : IPropertyTypeDefinition
{
    // enum stores ID as int, so we define it as int here
    public string Units => "";
    public Type SystemType => typeof(int);

    public bool Validate(object value, out string errorMessage)
    {
        errorMessage = string.Empty;
        return value is int;
    }
        
    public string FormatValue(object value) => value.ToString();

    public bool TryParse(string input, out object value)
    {
        if (int.TryParse(input, out var result))
        {
            value = result;
            return true;
        }
        value = default(int);
        return false;
    }
}