using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class EnumDefinition : IPropertyTypeDefinition
{
    // enum stores ID as int, so we define it as int here
    public string Units => "";
    public Type SystemType => typeof(int);

    public (bool Success, string errorMessage) Validate(object value)
    {
        return (value is int, "");
    }
        
    public string FormatValue(object value) => value.ToString();

    public (bool Success, object Value) TryParse(string input)
    {
        if (int.TryParse(input, out var result))
            return (true, result);
        
        return (false, 0);
    }
}