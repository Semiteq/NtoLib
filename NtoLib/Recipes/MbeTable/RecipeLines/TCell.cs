using System;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal class TCell
    {
        public CellType Type { get; }

        private bool _boolValue;
        private double _floatValue;
        private int _intValue;
        private string _stringValue;

        #region Constructors

        public TCell(CellType type, string value)
        {
            Type = type;
            ParseValue(value);
        }

        public TCell(CellType type, string value, int intValue)
        {
            Type = type;
            UIntValue = (uint)intValue;
            ParseValue(value);
        }

        public TCell(CellType type, int value)
        {
            Type = type;
            ParseValue(value);
            _intValue = value;
        }

        public TCell(CellType type, float value)
        {
            Type = type;
            ParseValue(value);
            _floatValue = value;
        }

        #endregion

        #region Conversions to different types

        public int IntValue
        {
            get => Type == CellType._bool ? (_boolValue ? 1 : 0) :
                   Type == CellType._float ? (int)_floatValue : _intValue;
        }

        public uint UIntValue
        {
            // Работа с целочисленным представлением значений
            get => Type == CellType._bool ? (_boolValue ? 1U : 0U) :
                   (Type == CellType._int || Type == CellType._enum) ? (uint)_intValue :
                   BitConverter.ToUInt32(BitConverter.GetBytes((float)_floatValue), 0);

            set
            {
                if (Type == CellType._bool) _boolValue = value != 0U;

                else if (Type == CellType._int || Type == CellType._enum) _intValue = (int)value;

                else _floatValue = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            }
        }

        public double FloatValue
        {
            get => Type == CellType._bool ? (_boolValue ? 1.0 : 0.0) :
                   (Type == CellType._int || Type == CellType._enum) ? (double)_intValue : _floatValue;
        }

        public bool BoolValue
        {
            //Проверка не boolean переменной на TRUE или FALSE
            get => Type == CellType._int || Type == CellType._enum ? _intValue != 0 : (Type == CellType._float ? this._floatValue != 0.0 : this._boolValue);
        }

        public string StringValue
        {
            get =>
                Type == CellType._bool ? _boolValue.ToString() :
                Type == CellType._int ? _intValue.ToString() :

                ((Type == CellType._float) ||
                (Type == CellType._floatPercent) ||
                (Type == CellType._floatTemp) ||
                (Type == CellType._floatSecond) ||
                (Type == CellType._floatTempSpeed) ||
                (Type == CellType._floatPowerSpeed)) ? _floatValue.ToString("F5") :

                (Type == CellType._enum || Type == CellType._string) ? _stringValue :
                _stringValue;
        }

        #endregion

        //public override string ToString() => GetValue();

        public void ParseValue(string value)
        {
            string upperValue = value?.ToUpper();

            if (Type == CellType._bool)
            {
                if (upperValue == "TRUE" || upperValue == "ДА" || upperValue == "YES" || upperValue == "ON" || upperValue == "1")
                    _boolValue = true;
                else if (upperValue == "FALSE" || upperValue == "НЕТ" || upperValue == "NO" || upperValue == "OFF" || upperValue == "0")
                    _boolValue = false;
                else
                    throw new Exception($"Wrong value(BoolType): \"{value}\"");
            }
            else if (Type == CellType._floatSecond)
            {
                if (!DateTime.TryParse(value, out var dateTime))
                    throw new Exception($"Wrong value(TimeValue): \"{value}\"");

                _floatValue = (float)dateTime.Second + (float)dateTime.Millisecond / 1000;
            }
            else if (Type == CellType._float || Type == CellType._floatTemp || Type == CellType._floatPercent || Type == CellType._floatTempSpeed || Type == CellType._floatPowerSpeed)
            {
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out this._floatValue))
                    throw new Exception($"Wrong value(FloatType): \"{value}\"");
            }
            else if (Type == CellType._int)
            {
                if (!int.TryParse(value, out this._intValue))
                    throw new Exception($"Wrong value(IntType): \"{value}\"");
            }
            else if (Type == CellType._enum)
            {
                if (!string.IsNullOrEmpty(value))
                    _stringValue = value;
                else
                    throw new Exception($"Wrong value(EnumType): \"{value}\"");
            }
            else if (Type == CellType._string || Type == CellType._blocked)
            {
                if (value != null)
                    _stringValue = value;
                else
                    throw new Exception($"Wrong value(StringType): \"{value}\"");
            }
            else
            {
                throw new Exception("Unknown cell type");
            }
        }


        public void ParseValue(int value)
        {
            _intValue = value;
        }

        public void ParseValue(float value)
        {
            _floatValue = value;
        }

        // Получение строки с форматированным значением
        public string GetValue()
        {
            if (Type == CellType._bool) return _boolValue.ToString();
            if (Type == CellType._float) return _floatValue.ToString("F5");
            if (Type == CellType._floatTemp) return $"{_floatValue:F0} ⁰C";
            if (Type == CellType._floatPercent) return $"{_floatValue:F1} %";
            if (Type == CellType._floatSecond) return FormatTime(_floatValue);
            if (Type == CellType._floatTempSpeed) return $"{_floatValue:F1} ⁰C/мин";
            if (Type == CellType._floatPowerSpeed) return $"{_floatValue:F2} %/мин";
            if (Type == CellType._int) return _intValue.ToString();
            if (Type == CellType._string || Type == CellType._enum) return _stringValue;
            return string.Empty;
        }

        private string FormatTime(double time)
        {
            return TimeSpan.FromSeconds(time).ToString("hh\\:mm\\:ss\\.ff");
        }
    }
}
