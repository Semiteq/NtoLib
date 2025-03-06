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
            get => Type == CellType.Bool ? (_boolValue ? 1 : 0) :
                   Type == CellType.Float ? (int)_floatValue : _intValue;
        }

        public uint UIntValue
        {
            // Работа с целочисленным представлением значений
            get => Type == CellType.Bool ? (_boolValue ? 1U : 0U) :
                   (Type == CellType.Int || Type == CellType.Enum) ? (uint)_intValue :
                   BitConverter.ToUInt32(BitConverter.GetBytes((float)_floatValue), 0);

            set
            {
                if (Type == CellType.Bool) _boolValue = value != 0U;

                else if (Type == CellType.Int || Type == CellType.Enum) _intValue = (int)value;

                else _floatValue = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            }
        }

        public float FloatValue
        {
            get => (float)(Type == CellType.Bool ? (_boolValue ? 1.0 : 0.0) :
                   (Type == CellType.Int || Type == CellType.Enum) ? _intValue : _floatValue);
        }

        public bool BoolValue
        {
            //Проверка не boolean переменной на TRUE или FALSE
            get => Type == CellType.Int || Type == CellType.Enum ? _intValue != 0 : (Type == CellType.Float ? this._floatValue != 0.0 : this._boolValue);
        }

        public string StringValue
        {
            get =>
                Type == CellType.Bool ? _boolValue.ToString() :
                Type == CellType.Int ? _intValue.ToString() :

                ((Type == CellType.Float) ||
                (Type == CellType.FloatPercent) ||
                (Type == CellType.FloatTemp) ||
                (Type == CellType.FloatSecond) ||
                (Type == CellType.FloatTempSpeed) ||
                (Type == CellType.FloatPowerSpeed)) ? _floatValue.ToString("F5") :

                (Type == CellType.Enum || Type == CellType.String) ? _stringValue :
                _stringValue;
        }

        #endregion

        //public override string ToString() => GetValue();

        public void ParseValue(string value)
        {
            string upperValue = value?.ToUpper();

            if (Type == CellType.Bool)
            {
                _boolValue = upperValue == "TRUE" || upperValue == "ДА" || upperValue == "YES" || upperValue == "ON" || upperValue == "1"
                        || (upperValue == "FALSE" || upperValue == "НЕТ" || upperValue == "NO" || upperValue == "OFF" || upperValue == "0"
                    ? false
                    : throw new Exception($"Wrong value(BoolType): \"{value}\""));
            }
            else if (Type == CellType.FloatSecond)
            {
                if (!DateTime.TryParse(value, out var dateTime))
                    throw new Exception($"Wrong value(TimeValue): \"{value}\"");

                _floatValue = (float)dateTime.Second + (float)dateTime.Millisecond / 1000;
            }
            else if (Type == CellType.Float || Type == CellType.FloatTemp || Type == CellType.FloatPercent || Type == CellType.FloatTempSpeed || Type == CellType.FloatPowerSpeed)
            {
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out this._floatValue))
                    throw new Exception($"Wrong value(FloatType): \"{value}\"");
            }
            else if (Type == CellType.Int)
            {
                if (!int.TryParse(value, out this._intValue))
                    throw new Exception($"Wrong value(IntType): \"{value}\"");
            }
            else
            {
                _stringValue = Type == CellType.Enum
                    ? !string.IsNullOrEmpty(value) ? value : throw new Exception($"Wrong value(EnumType): \"{value}\"")
                    : Type == CellType.String || Type == CellType.Blocked
                                    ? value != null ? value : throw new Exception($"Wrong value(StringType): \"{value}\"")
                                    : throw new Exception("Unknown cell type");
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
            return Type == CellType.Bool
                ? _boolValue.ToString()
                : Type == CellType.Float
                ? _floatValue.ToString("F2")
                : Type == CellType.FloatTemp
                ? $"{_floatValue:F0} ⁰C"
                : Type == CellType.FloatPercent
                ? $"{_floatValue:F1} %"
                : Type == CellType.FloatSecond
                ? FormatTime(_floatValue)
                : Type == CellType.FloatTempSpeed
                ? $"{_floatValue:F1} ⁰C/мин"
                : Type == CellType.FloatPowerSpeed
                ? $"{_floatValue:F2} %/мин"
                : Type == CellType.Int ? _intValue.ToString() : Type == CellType.String || Type == CellType.Enum ? _stringValue : string.Empty;
        }

        private string FormatTime(double time)
        {
            return TimeSpan.FromSeconds(time).ToString("hh\\:mm\\:ss\\.ff");
        }
    }
}
