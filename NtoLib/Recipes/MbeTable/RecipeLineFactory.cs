using NtoLib.Recipes.MbeTable.TableLines;
using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeLineFactory
    {
        private readonly Dictionary<string, Func<RecipeLine>> simpleCreators = new()
        {
            { Close.ActionName, () => new Close() },
            { Open.ActionName, () => new Open() },
            { OpenTime.ActionName, () => new OpenTime() },
            { CloseAll.ActionName, () => new CloseAll() },

            { Temperature.ActionName, () => new Temperature() },
            { TemperatureWait.ActionName, () => new TemperatureWait() },
            { TemperatureBySpeed.ActionName, () => new TemperatureBySpeed() },
            { TemperatureByTime.ActionName, () => new TemperatureByTime() },
            { Power.ActionName, () => new Power() },
            { PowerWait.ActionName, () => new PowerWait() },
            { PowerBySpeed.ActionName, () => new PowerBySpeed() },
            { PowerByTime.ActionName, () => new PowerByTime() },

            { Wait.ActionName, () => new Wait() },
            { For_Loop.ActionName, () => new For_Loop() },
            { EndFor_Loop.ActionName, () => new EndFor_Loop() },
            { Pause.ActionName, () => new Pause() }
        };

        private readonly Dictionary<string, Func<int, float, float, string, RecipeLine>> parameterizedCreators = new()
        {
            { Close.ActionName, (number, _, _, comment) => new Close(number, comment) },
            { Open.ActionName, (number, _, _, comment) => new Open(number, comment) },
            { OpenTime.ActionName, (number, _, timeSetpoint, comment) => new OpenTime(number, timeSetpoint, comment) },
            { CloseAll.ActionName, (_, _, _, comment) => new CloseAll(comment) },

            { Temperature.ActionName, (number, setpoint, _, comment) => new Temperature(number, setpoint, comment) },
            { TemperatureWait.ActionName, (number, setpoint, timeSetpoint, comment) => new TemperatureWait(number, setpoint, timeSetpoint, comment) },
            { TemperatureBySpeed.ActionName, (number, setpoint, timeSetpoint, comment) => new TemperatureBySpeed(number, setpoint, timeSetpoint, comment) },
            { TemperatureByTime.ActionName, (number, setpoint, timeSetpoint, comment) => new TemperatureByTime(number, setpoint, timeSetpoint, comment) },
            { Power.ActionName, (number, setpoint, _, comment) => new Power(number, setpoint, comment) },
            { PowerWait.ActionName, (number, setpoint, timeSetpoint, comment) => new PowerWait(number, setpoint, timeSetpoint, comment) },
            { PowerBySpeed.ActionName, (number, setpoint, timeSetpoint, comment) => new PowerBySpeed(number, setpoint, timeSetpoint, comment) },
            { PowerByTime.ActionName, (number, setpoint, timeSetpoint, comment) => new PowerByTime(number, setpoint, timeSetpoint, comment) },

            { Wait.ActionName, (_, _, timeSetpoint, comment) => new Wait(timeSetpoint, comment) },
            { For_Loop.ActionName, (number, _, _, comment) => new For_Loop((int)number, comment) },
            { EndFor_Loop.ActionName, (_, _, _, comment) => new EndFor_Loop(comment) },
            { Pause.ActionName, (_, _, _, comment) => new Pause(comment) }
        };

        public RecipeLine NewLine(string command)
        {
            return simpleCreators.TryGetValue(command, out var creator) ? creator() : null;
        }

        public RecipeLine NewLine(string command, int number, float setpoint, float timeSetpoint, string comment)
        {
            return parameterizedCreators.TryGetValue(command, out var creator) ? creator(number, setpoint, timeSetpoint, comment) : null;
        }

        public RecipeLine NewLine(ushort[] int_data, ushort[] float_data, ushort[] bool_data, int index)
        {
            string command = RecipeLine.Actions.GetValueByIndex((int)int_data[index * 2]).ToString();
            int number = (int)int_data[index * 2 + 1];

            float setpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4] + ((uint)float_data[index * 4 + 1] << 16)), 0);
            float timeSetpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4 + 2] + ((uint)float_data[index * 4 + 3] << 16)), 0);

            return NewLine(command, number, setpoint, timeSetpoint, "");
        }
    }
}
