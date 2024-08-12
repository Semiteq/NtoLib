using InSAT.Library.Linq;
using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class TCell
    {
        public CellType Type { get => _type; }

        private bool boolValue;
        private double floatValue;
        private int intValue;
        private string stringValue;

        private CellType _type;

        public TCell(CellType type, string value)
        {
            _type = type;
            this.SetNewValue(value);
        }

        public TCell(CellType type, string value, int intValue)
        {
            _type = type;
            UIntValue = (uint)intValue;
            this.SetNewValue(value);
        }


        public TCell(CellType type, float value)
        {
            _type = type;
            floatValue = value;
        }

        public TCell(CellType type, int value)
        {
            _type = type;
            intValue = value;
        }

        public TCell(CellType type, byte[] buf, ref int offset)
        {
            _type = type;
            if (Type == CellType._bool)
                boolValue = BitConverter.ToUInt32(buf, offset) != 0U;

            if (Type == CellType._float)
                floatValue = (double)BitConverter.ToSingle(buf, offset);

            if (Type == CellType._int || Type == CellType._enum)
                intValue = BitConverter.ToInt32(buf, offset);

            offset += 4;
        }

        public uint servalue
        {
            get => Type == CellType._bool ? (!boolValue ? 0U : 1U) : (Type == CellType._int || Type == CellType._enum ? (uint)intValue : BitConverter.ToUInt32(BitConverter.GetBytes((float)floatValue), 0));
            set
            {
                if (Type == CellType._bool)
                    this.boolValue = value != 0U;
                else if (Type == CellType._int || Type == CellType._enum)
                    this.intValue = (int)value;
                else
                    this.floatValue = (double)BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            }
        }



        public bool BoolValue
        {
            get
            {
                if (Type == CellType._int || Type == CellType._enum)
                    return this.intValue != 0;
                return Type == CellType._float ? this.floatValue != 0.0 : this.boolValue;
            }
        }
        public uint UIntValue
        {
            get => Type == CellType._bool ? (!this.boolValue ? 0U : 1U) : (Type == CellType._int || Type == CellType._enum ? (uint)this.intValue : BitConverter.ToUInt32(BitConverter.GetBytes((float)this.floatValue), 0));
            set
            {
                if (Type == CellType._bool)
                    this.boolValue = value != 0U;
                else if (Type == CellType._int || Type == CellType._enum)
                    this.intValue = (int)value;
                else
                    this.floatValue = (double)BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
            }
        }
        public int IntValue => Type == CellType._bool ? (this.boolValue ? 1 : 0) : (Type == CellType._float ? (int)this.floatValue : this.intValue);
        public double FloatValue => Type == CellType._bool ? (this.boolValue ? 1.0 : 0.0) : (Type == CellType._int || Type == CellType._enum ? (double)this.intValue : this.floatValue);
        public string StringValue
        {
            get
            {
                if (Type == CellType._bool)
                    return boolValue.ToString();

                else if (Type == CellType._int)
                    return intValue.ToString();

                else if ((Type == CellType._float) ||
                    (Type == CellType._floatPercent) ||
                    (Type == CellType._floatTemp) ||
                    (Type == CellType._floatSecond) ||
                    (Type == CellType._floatTempSpeed) ||
                    (Type == CellType._floatPowerSpeed))
                    return floatValue.ToString("F5");

                else if ((Type == CellType._enum) ||
                    (Type == CellType._string))
                    return stringValue;
                return stringValue;
            }
        }
        public override string ToString() => this.StringValue;


        public void SetNewValue(string value)
        {
            if (Type == CellType._bool)
            {
                if (value.ToUpper() == "TRUE" || value.ToUpper() == "ДА" || value.ToUpper() == "YES" || value.ToUpper() == "ON" || value.ToUpper() == "1")
                    boolValue = true;
                else if (value.ToUpper() == "FALSE" || value.ToUpper() == "НЕТ" || value.ToUpper() == "NO" || value.ToUpper() == "OFF" || value.ToUpper() == "0")
                    boolValue = false;
                else
                    throw new Exception("Wrong value(BoolType): \"" + value + "\"");
            }

            else if (Type == CellType._float || Type == CellType._floatTemp || Type == CellType._floatPercent || Type == CellType._floatSecond || Type == CellType._floatTempSpeed || Type == CellType._floatPowerSpeed)
            {
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out this.floatValue))
                    throw new Exception("Wrong value(FloatType): \"" + value + "\"");
            }

            else if (Type == CellType._int)
            {
                if (!int.TryParse(value, out this.intValue))
                    throw new Exception("Wrong value(IntType): \"" + value + "\"");
            }

            else if (Type == CellType._enum)
            {
                if (!value.IsEmpty() && value != null)
                    stringValue = value;
                else
                    throw new Exception("Wrong value(EnumType): \"" + value + "\"");
            }
            else if (Type == CellType._string || Type == CellType._blocked)
            {
                if (value != null)
                    stringValue = value;
                else
                    throw new Exception("Wrong value(StringType): \"" + value + "\"");
            }
            else
            {
                throw new Exception("Unknown cell type");
            }
        }

        public void SetNewValue(int value)
        {
            intValue = value;
        }

        public void SetNewValue(float value)
        { 
            floatValue = value;
        }

        public string GetValue()
        {
            if (Type == CellType._bool)
                return boolValue.ToString();

            else if (Type == CellType._float)
                return floatValue.ToString();

            else if (Type == CellType._floatTemp)
                return string.Format("{0:f0}", floatValue) + " ⁰C";

            else if (Type == CellType._floatPercent)
                return string.Format("{0:f1}", floatValue) + " %";

            else if (Type == CellType._floatSecond)
                return FormatTime(floatValue);

            else if (Type == CellType._floatTempSpeed)
                return string.Format("{0:f1}", floatValue)  + " ⁰C/мин";

            else if (Type == CellType._floatPowerSpeed)
                return string.Format("{0:f2}", floatValue) + " %/мин";

            else if (Type == CellType._int)
                return intValue.ToString();

            else if (Type == CellType._enum || Type == CellType._string)
                return stringValue;

            else
                return String.Empty;
        }

        private string FormatTime(double time)
        {
            if (time < 60.0f)
            {
                return TimeSpan.FromSeconds(time).ToString(@"s\.ff") + " c";
            }
            else if (time < 3600.0f)
            {
                return TimeSpan.FromSeconds(time).ToString(@"m\:ss\.ff") + " c";
            }
            else 
            {
                return TimeSpan.FromSeconds(time).ToString(@"h\:mm\:ss\.ff") + " c";
            }

            /*TimeSpan timeSpan = TimeSpan.FromSeconds(time);

            if (timeSpan.TotalHours >= 1)
                return timeSpan.ToString(@"hh\:mm\:ss\.f");
            else if (timeSpan.TotalMinutes >= 1)
                return timeSpan.ToString(@"m\:ss\.f");
            else
                return timeSpan.ToString(@"s\.f");


            double miliseconds = time % 1.0f;

            int seconds = (int)((time - miliseconds) % 60);
            int minutes = (int)((time - miliseconds) / 60);

            string ms = miliseconds != 0 ? "," + String.Format("{0:f0}", miliseconds * 1000) : "";
            ms = ms.TrimEnd('0');

            string s = seconds != 0 ? seconds + ms + "s" : "0" + ms + "s";
            string m = minutes != 0 ? minutes + "m" : "";

            return m + s;*/
        }
    }
}