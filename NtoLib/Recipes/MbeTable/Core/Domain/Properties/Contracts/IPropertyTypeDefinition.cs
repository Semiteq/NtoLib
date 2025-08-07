using System;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

public interface IPropertyTypeDefinition
{
    string Units { get; }

    Type SystemType { get; }

    bool Validate(object value, out string errorMessage);
    
    string FormatValue(object value);

    bool TryParse(string input, out object value);
}