using System;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Contracts;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions
{
    public class IntDefinition : IPropertyTypeDefinition
    {
        public string Units => "";
        public Type SystemType => typeof(int);

        public bool Validate(object value, out string errorMessage)
        {
            errorMessage = string.Empty;
            // Проверка на MinValue/MaxValue не требуется, т.к. соответствует границам типа.
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
}