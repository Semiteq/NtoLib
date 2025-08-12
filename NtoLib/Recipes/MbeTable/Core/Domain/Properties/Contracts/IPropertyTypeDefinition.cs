using System;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

public interface IPropertyTypeDefinition
{
    string Units { get; }

    Type SystemType { get; }

    (bool Success, string errorMessage) Validate(object value);
    
    string FormatValue(object value);

    (bool Success, object Value) TryParse(string input);
}