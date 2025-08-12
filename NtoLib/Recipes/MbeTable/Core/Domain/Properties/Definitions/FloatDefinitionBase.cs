using System;
using System.Globalization;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public abstract class FloatDefinitionBase : IPropertyTypeDefinition
    {
        public abstract string Units { get; }
        public Type SystemType => typeof(float);
        public abstract float MinValue { get; }
        public abstract float MaxValue { get; }
        public abstract string MinMaxErrorMessage { get; }

        public virtual (bool Success, string errorMessage) Validate(object value)
        {
            if (value is not float floatValue)
                return (false, "Ожидалось значение типа float.");
            
            if (floatValue < MinValue || floatValue > MaxValue)
                return (false, MinMaxErrorMessage);
            

            return (true, "");
        }

        public virtual string FormatValue(object value)
        {
            return ((float)value).ToString(CultureInfo.InvariantCulture);
        }
        
        public virtual (bool Success, object Value) TryParse(string input)
        {
            // Drop all non-chars and replace "," with "."
            var sanitizedInput = new string(input.Trim().Where(c => char.IsDigit(c) || c == ',').ToArray()).Replace(',', '.');
            
            return float.TryParse(sanitizedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) 
                ? (true, result) 
                : (false, 0f);
        }
    }
}