using System;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions;

public class BoolDefinition : IPropertyTypeDefinition
{
    public string Units => "";
    public Type SystemType => typeof(bool);

    public (bool Success, string errorMessage) Validate(object value)
    {
        return (value is bool, ""); // Always valid if bool
    }
        
    public string FormatValue(object value) => value.ToString();

    public (bool Success, object Value) TryParse(string input)
    {
        if (bool.TryParse(input, out var result))
            return (true ,result);
        
        
        return (false, false);
    }
}