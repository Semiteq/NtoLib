using NtoLib.Recipes.MbeTable.TableLines;
using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeLineFactory
    {
        public RecipeLineFactory() { }

        public RecipeLine NewLine(string command)
        {
            if(command == Close.Name)
                return new Close();
            else if(command == CloseAll.Name)
                return new CloseAll();
            else if(command == Open.Name)
                return new Open();
            else if(command == OpenTime.Name)
                return new OpenTime();

            else if(command == Temperature.Name)
                return new Temperature();
            else if(command == TemperatureWait.Name)
                return new TemperatureWait();
            else if(command == TemperatureBySpeed.Name)
                return new TemperatureBySpeed();
            else if(command == TemperatureByTime.Name)
                return new TemperatureByTime();

            else if(command == Power.Name)
                return new Power();
            else if(command == PowerWait.Name)
                return new PowerWait();
            else if(command == PowerBySpeed.Name)
                return new PowerBySpeed();
            else if(command == PowerByTime.Name)
                return new PowerByTime();

            else if(command == Wait.Name)
                return new Wait();

            else if(command == For_Loop.Name)
                return new For_Loop();
            else if(command == EndFor_Loop.Name)
                return new EndFor_Loop();
            else if(command == Pause.Name)
                return new Pause();

            else
                return null;
        }

        public RecipeLine NewLine(string command, int number, float setpoint, float timeSetpoint, string comment)
        {
            if(command == Close.Name)
                return new Close(number, comment);
            else if(command == CloseAll.Name)
                return new CloseAll(comment);
            else if(command == Open.Name)
                return new Open(number, comment);
            else if(command == OpenTime.Name)
                return new OpenTime(number, timeSetpoint, comment);

            else if(command == Temperature.Name)
                return new Temperature(number, setpoint, comment);
            else if(command == TemperatureWait.Name)
                return new TemperatureWait(number, setpoint, timeSetpoint, comment);
            else if(command == TemperatureBySpeed.Name)
                return new TemperatureBySpeed(number, setpoint, timeSetpoint, comment);
            else if(command == TemperatureByTime.Name)
                return new TemperatureByTime(number, setpoint, timeSetpoint, comment);

            else if(command == Power.Name)
                return new Power(number, setpoint, comment);
            else if(command == PowerWait.Name)
                return new PowerWait(number, setpoint, timeSetpoint, comment);
            else if(command == PowerBySpeed.Name)
                return new PowerBySpeed(number, setpoint, timeSetpoint, comment);
            else if(command == PowerByTime.Name)
                return new PowerByTime(number, setpoint, timeSetpoint, comment);

            else if(command == Wait.Name)
                return new Wait(timeSetpoint, comment);

            else if(command == For_Loop.Name)
                return new For_Loop((int)setpoint, comment);
            else if(command == EndFor_Loop.Name)
                return new EndFor_Loop(comment);
            else if(command == Pause.Name)
                return new Pause(comment);

            else
                return null;
        }

        public RecipeLine NewLine(ushort[] int_data, ushort[] float_data, ushort[] bool_data, int index)
        {
            string command = RecipeLine.Actions.GetValueByIndex((int)int_data[index * 2]).ToString();
            int number = (int)int_data[index * 2 + 1];

            float setpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4] + ((uint)float_data[index * 4 + 1] << 16)), 0);
            float timeSetpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4 + 2] + ((uint)float_data[index * 4 + 3] << 16)), 0);

            return NewLine(command, number, setpoint, timeSetpoint, "");
            /*
            for (int index = 0; index < _cells.Count; ++index)
            {
                if (_cells[index].colum.type == CellType._int || _cells[index].colum.type == CellType._enum)
                {
                    _cells[index].servalue = (uint)int_data[intIndex];
                    ++intIndex;
                }
                if (_cells[index].colum.type == CellType._float)
                {
                    _cells[index].servalue = (uint)float_data[floatIndex * 2] + ((uint)float_data[floatIndex * 2 + 1] << 16);
                    ++floatIndex;
                }
                if (_cells[index].colum.type == CellType._bool)
                {
                    _cells[index].servalue = (uint)((int)bool_data[boolIndex / 16] >> boolIndex % 16 & 1);
                    ++boolIndex;
                }
            }*/
        }
    }
}