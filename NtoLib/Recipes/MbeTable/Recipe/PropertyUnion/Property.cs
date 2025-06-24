using System;
using System.Linq;
using OneOf;

namespace NtoLib.Recipes.MbeTable.Recipe.PropertyUnion
{
    public class Property
    {
        public Property(PropertyType type, OneOf<bool, int, float, string> value)
        {
            Value = value;
            Type = type;
        }
        
        public Property(PropertyType type, bool value)
        {
            Value = value;
            Type = type;
        }

        public Property(PropertyType type, int value)
        {
            Value = value;
            Type = type;
        }

        public Property(PropertyType type, float value)
        {
            Value = value;
            Type = type;
        }

        public Property(PropertyType type, string value)
        {
            Value = value;
            Type = type;
        }

        public PropertyType Type { get; }
        public Type SystemType => PropertyTypeMapper.GetSystemType(Type);
        public bool IsBlocked => Type == PropertyType.Blocked;
        public string PropertyName { get; private set; }
        public bool BoolValue => Value.AsT0;
        public int IntValue => Value.AsT1;
        public float FloatValue => Value.AsT2;
        public string StringValue => Value.AsT3;
        private OneOf<bool, int, float, string> Value { get; set; }
        
        public bool SetValue(bool value, out string validationError)
        {
            OneOf<bool, int, float, string> unionValue = value;
            return SetValue(unionValue, out validationError);
        }

        public bool SetValue(int value, out string validationError)
        {
            OneOf<bool, int, float, string> unionValue = value;
            return SetValue(unionValue, out validationError);
        }

        public bool SetValue(float value, out string validationError)
        {
            OneOf<bool, int, float, string> unionValue = value;
            return SetValue(unionValue, out validationError);
        }

        public bool SetValue(string value, out string validationError)
        {
            OneOf<bool, int, float, string> unionValue = value;
            return SetValue(unionValue, out validationError);
        }

        public bool SetValue(OneOf<bool, int, float, string> value, out string validationError)
        {
            if (!Validate(value, out validationError))
                return false;

            Value = value;
            return true;
        }

        public string GetFormattedValue() => Value.Match(
            boolValue => boolValue.ToString(),
            intValue => intValue.ToString(),
            floatValue => floatValue.ToString("F2"),
            stringValue => stringValue
        );

        public string GetUnits() => PropertyUnitsMapper.GetUnits(Type);

        public string GetDisplayValue() => GetFormattedValue() + " " + GetUnits();

        public bool TryParseValue(string input, out OneOf<bool, int, float, string> result, out string validationError)
        {
            result = default;
            validationError = string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                validationError = "Входная строка пуста";
                return false;
            }

            var trimmedInput = input.Trim();

            switch (Type)
            {
                case PropertyType.Bool:
                    if (bool.TryParse(trimmedInput, out var boolVal))
                    {
                        result = boolVal;
                        return Validate(result, out validationError);
                    }
                    validationError = "Не удалось преобразовать в логическое значение";
                    break;
                case PropertyType.Int:
                    if (int.TryParse(trimmedInput, out var intVal))
                    {
                        result = intVal;
                        return Validate(result, out validationError);
                    }
                    validationError = "Не удалось преобразовать в целое число";
                    break;
                case PropertyType.Float:
                case PropertyType.Temp:
                case PropertyType.Flow:
                case PropertyType.Percent:
                case PropertyType.PowerSpeed:
                case PropertyType.TempSpeed:
                    if (TryParseFloat(trimmedInput, out var floatVal))
                    {
                        result = floatVal;
                        return Validate(result, out validationError);
                    }
                    validationError = "Не удалось преобразовать в число с плавающей точкой";
                    break;
                case PropertyType.Time:
                    if (TryParseTime(trimmedInput, out var timeVal))
                    {
                        result = timeVal;
                        return Validate(result, out validationError);
                    }
                    validationError = "Не удалось преобразовать во время. Используйте формат hh:mm:ss[.ms] или число";
                    break;
                case PropertyType.String:
                    result = trimmedInput;
                    return Validate(result, out validationError);
            }

            return false;
        }

        private bool TryParseFloat(string input, out float value)
        {
            value = 0;
            
            // Try parse regular numeric value
            var numericPart = new string(input.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            numericPart = numericPart.Replace('.', ',');
            return float.TryParse(numericPart, out value);
        }

        private bool TryParseTime(string input, out float value)
        {
            value = 0;
            
            // Try parse time format hh:mm:ss[.ms]
            if (input.Contains(':'))
            {
                var parts = input.Split(':');
                if (parts.Length >= 2)
                {
                    var hours = int.TryParse(parts[0], out var h) ? h : 0;
                    var minutes = int.TryParse(parts[1], out var m) ? m : 0;
                    var seconds = parts.Length > 2 && int.TryParse(parts[2].Split('.')[0], out var s) ? s : 0;
                    var milliseconds = parts.Length > 2 && parts[2].Contains('.') &&
                                       float.TryParse(("0" + parts[2].Split('.')[1]).Replace('.', ','), out var ms)
                        ? ms
                        : 0;

                    value = hours * 3600 + minutes * 60 + seconds + milliseconds / 1000;
                    return true;
                }
            }

            // Fallback to regular float parsing
            return TryParseFloat(input, out value);
        }

        private bool Validate(OneOf<bool, int, float, string> value, out string validationError)
        {
            validationError = string.Empty;

            var limits = PropertyLimitsMapper.GetLimits(Type);
            if (!limits.HasValue)
            {
                return true;
            }

            var (min, max) = limits.Value;

            if (value.IsT0) // bool
            {
                return true; // bool always valid
            }
            else if (value.IsT1) // int
            {
                var intValue = value.AsT1;
                if (intValue < min || intValue > max)
                {
                    validationError = $"Целое число должно быть в пределах от {min} до {max}";
                    return false;
                }
                return true;
            }
            else if (value.IsT2) // float
            {
                var floatValue = value.AsT2;
                if (floatValue < min || floatValue > max)
                {
                    validationError = $"Число должно быть в пределах от {min} до {max}";
                    return false;
                }
                return true;
            }
            else if (value.IsT3) // string
            {
                var stringValue = value.AsT3;
                if (stringValue.Length < min || stringValue.Length > max)
                {
                    validationError = $"Длина строки должна быть от {min} до {max} символов";
                    return false;
                }
                return true;
            }

            return true;
        }
    }
}