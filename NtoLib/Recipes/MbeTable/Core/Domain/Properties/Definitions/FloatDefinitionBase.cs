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
            var floatValue = (float)value;

            if (floatValue != 0 && (Math.Abs(floatValue) < 0.001 || Math.Abs(floatValue) > 10000))
                return floatValue.ToString("0.##E+0", CultureInfo.InvariantCulture);

            return floatValue % 1 == 0
                ? floatValue.ToString("F0", CultureInfo.InvariantCulture)
                : floatValue.ToString("G2", CultureInfo.InvariantCulture);
        }

        public virtual (bool Success, object Value) TryParse(string input)
        {
            // Drop all non-chars and replace "," with "."
            var sanitizedInput = new string(input.Trim().Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                .Replace(',', '.');

            if (float.TryParse(sanitizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return (true, result);

            return (false, 0f);
        }
    }
}