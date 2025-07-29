using System;
using System.Globalization;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Contracts;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyDataType.Definitions
{
    public abstract class FloatDefinitionBase : IPropertyTypeDefinition
    {
        public abstract string Units { get; }
        public Type SystemType => typeof(float);

        protected abstract float MinValue { get; }
        protected abstract float MaxValue { get; }
        protected abstract string MinMaxErrorMessage { get; }

        public bool Validate(object value, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (value is not float floatValue)
            {
                errorMessage = "Ожидалось значение типа float.";
                return false;
            }

            if (floatValue < MinValue || floatValue > MaxValue)
            {
                errorMessage = MinMaxErrorMessage;
                return false;
            }

            return true;
        }

        public string FormatValue(object value)
        {
            return ((float)value).ToString(CultureInfo.InvariantCulture);
        }

        public bool TryParse(string input, out object value)
        {
            if (float.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                value = result;
                return true;
            }
            value = default(float);
            return false;
        }
    }
}