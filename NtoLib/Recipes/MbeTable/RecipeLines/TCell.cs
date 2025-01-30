using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class TCell
    {
        public CellType Type { get; }

        private bool boolValue;
        private double floatValue;
        private int intValue;
        private string stringValue;

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
            intValue = value;
        }

        public TCell(CellType type, float value)
        {
            Type = type;
            ParseValue(value);
            floatValue = value;
        }

        //public TCell(CellType type, byte[] buffer, ref int offset)
        //{
        //    Type = type;
        //    ParseBuffer(buffer, ref offset);
        //}

        #endregion

        #region Conversions to different types

        public int IntValue
        {
            get => Type == CellType._bool ? (boolValue ? 1 : 0) :
                   Type == CellType._float ? (int)floatValue : intValue;
        }

        public uint UIntValue
        {
            // Работа с целочисленным представлением значений
            get => Type == CellType._bool ? (boolValue ? 1U : 0U) :
                   (Type == CellType._int || Type == CellType._enum) ? (uint)intValue :
                   BitConverter.ToUInt32(BitConverter.GetBytes((float)floatValue), 0);

            set
            {
                if (Type == CellType._bool) boolValue = value != 0U;

                else if (Type == CellType._int || Type == CellType._enum) intValue = (int)value;

                else floatValue = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            }
        }

        public double FloatValue
        {
            get => Type == CellType._bool ? (boolValue ? 1.0 : 0.0) :
                   (Type == CellType._int || Type == CellType._enum) ? (double)intValue : floatValue;
        }

        public bool BoolValue
        {
            //Проверка не boolean переменной на TRUE или FALSE
            get => Type == CellType._int || Type == CellType._enum ? intValue != 0 : (Type == CellType._float ? this.floatValue != 0.0 : this.boolValue);
        }

        public string StringValue
        {
            get =>
                Type == CellType._bool ? boolValue.ToString() :
                Type == CellType._int ? intValue.ToString() :

                ((Type == CellType._float) ||
                (Type == CellType._floatPercent) ||
                (Type == CellType._floatTemp) ||
                (Type == CellType._floatSecond) ||
                (Type == CellType._floatTempSpeed) ||
                (Type == CellType._floatPowerSpeed)) ? floatValue.ToString("F5") :

                (Type == CellType._enum || Type == CellType._string) ? stringValue :
                stringValue;
        }

        #endregion

        //public override string ToString() => GetValue();

        public void ParseValue(string value)
        {
            string upperValue = value?.ToUpper();

            if (Type == CellType._bool)
            {
                if (upperValue == "TRUE" || upperValue == "ДА" || upperValue == "YES" || upperValue == "ON" || upperValue == "1")
                    boolValue = true;
                else if (upperValue == "FALSE" || upperValue == "НЕТ" || upperValue == "NO" || upperValue == "OFF" || upperValue == "0")
                    boolValue = false;
                else
                    throw new Exception($"Wrong value(BoolType): \"{value}\"");
            }
            else if (Type == CellType._floatSecond)
            {
                if (!DateTime.TryParse(value, out var dateTime))
                    throw new Exception($"Wrong value(TimeValue): \"{value}\"");

                floatValue = (float)dateTime.Second + (float)dateTime.Millisecond / 1000;
            }
            else if (Type == CellType._float || Type == CellType._floatTemp || Type == CellType._floatPercent || Type == CellType._floatTempSpeed || Type == CellType._floatPowerSpeed)
            {
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out this.floatValue))
                    throw new Exception($"Wrong value(FloatType): \"{value}\"");
            }
            else if (Type == CellType._int)
            {
                if (!int.TryParse(value, out this.intValue))
                    throw new Exception($"Wrong value(IntType): \"{value}\"");
            }
            else if (Type == CellType._enum)
            {
                if (!string.IsNullOrEmpty(value))
                    stringValue = value;
                else
                    throw new Exception($"Wrong value(EnumType): \"{value}\"");
            }
            else if (Type == CellType._string || Type == CellType._blocked)
            {
                if (value != null)
                    stringValue = value;
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
            intValue = value;
        }

        public void ParseValue(float value)
        {
            floatValue = value;
        }

        //// Парсинг данных из буфера
        //private void ParseBuffer(byte[] buffer, ref int offset)
        //{
        //    if (Type == CellType._bool) boolValue = BitConverter.ToUInt32(buffer, offset) != 0;

        //    else if (Type == CellType._float) floatValue = BitConverter.ToSingle(buffer, offset);

        //    else if (Type == CellType._int || Type == CellType._enum) intValue = BitConverter.ToInt32(buffer, offset);

        //    else throw new NotSupportedException($"Unsupported type for buffer parsing: {Type}");

        //    offset += 4;
        //}


        // Получение строки с форматированным значением
        public string GetValue()
        {
            if (Type == CellType._bool) return boolValue.ToString();
            if (Type == CellType._float) return floatValue.ToString("F5");
            if (Type == CellType._floatTemp) return $"{floatValue:F0} ⁰C";
            if (Type == CellType._floatPercent) return $"{floatValue:F1} %";
            if (Type == CellType._floatSecond) return FormatTime(floatValue);
            if (Type == CellType._floatTempSpeed) return $"{floatValue:F1} ⁰C/мин";
            if (Type == CellType._floatPowerSpeed) return $"{floatValue:F2} %/мин";
            if (Type == CellType._int) return intValue.ToString();
            if (Type == CellType._string || Type == CellType._enum) return stringValue;
            return string.Empty;
        }

        private string FormatTime(double time)
        {
            return TimeSpan.FromSeconds(time).ToString("hh\\:mm\\:ss\\.ff");
        }
    }
}
