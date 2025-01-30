using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.TableLines;
using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class RecipeLineFactory
    {
        private readonly Dictionary<string, Func<int, float, float, string, RecipeLine>> creators = new()
        {
            { Close.ActionName,              (n, _, _, c) => new Close              (n, c) },
            { Open.ActionName,               (n, _, _, c) => new Open               (n, c) },
            { OpenTime.ActionName,           (n, _, t, c) => new OpenTime           (n, t > 0f ? t : 1f, c) },
            { CloseAll.ActionName,           (_, _, _, c) => new CloseAll           (c) },

            { Temperature.ActionName,        (n, s, _, c) => new Temperature        (n, s > 0f ? s : 500f, c) },
            { TemperatureWait.ActionName,    (n, s, t, c) => new TemperatureWait    (n, s > 0f ? s : 500f, t > 0f ? t : 60f, c) },
            { TemperatureBySpeed.ActionName, (n, s, t, c) => new TemperatureBySpeed (n, s > 0f ? s : 500f, t > 0f ? t : 1f, c) },
            { TemperatureByTime.ActionName,  (n, s, t, c) => new TemperatureByTime  (n, s > 0f ? s : 500f, t > 0f ? t : 60f, c) },
            { Power.ActionName,              (n, s, _, c) => new Power              (n, s > 0f ? s : 10f, c) },
            { PowerWait.ActionName,          (n, s, t, c) => new PowerWait          (n, s > 0f ? s : 10f, t > 0f ? t : 60f, c) },
            { PowerBySpeed.ActionName,       (n, s, t, c) => new PowerBySpeed       (n, s > 0f ? s : 10f, t > 0f ? t : 1f, c) },
            { PowerByTime.ActionName,        (n, s, t, c) => new PowerByTime        (n, s > 0f ? s : 10f, t > 0f ? t : 1f, c) },

            { Wait.ActionName,               (_, _, t, c) => new Wait               (t > 0f ? t : 10f, c) },
            { For_Loop.ActionName,           (n, _, _, c) => new For_Loop           (n > 0 ? n : 5, c) },
            { EndFor_Loop.ActionName,        (_, _, _, c) => new EndFor_Loop        (c) },
            { Pause.ActionName,              (_, _, _, c) => new Pause              (c) },

            { NH3_Open.ActionName,           (_, _, _, c) => new NH3_Open           (c) },
            { NH3_Close.ActionName,          (_, _, _, c) => new NH3_Close          (c) },
            { NH3_Purge.ActionName,          (_, _, _, c) => new NH3_Purge          (c) }
        };

        public RecipeLine NewLine(string command, int number, float setpoint, float timeSetpoint, string comment)
        {
            return creators.TryGetValue(command, out var creator) ?
                   creator(number, setpoint, timeSetpoint, comment) : null;
        }

        public RecipeLine NewLine(ushort[] int_data, ushort[] float_data, ushort[] bool_data, int index)
        {
            string command = ActionManager.Names.GetValueByIndex((int)int_data[index * 2]).ToString();
            int number = (int)int_data[index * 2 + 1];

            float setpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4] + ((uint)float_data[index * 4 + 1] << 16)), 0);
            float timeSetpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)float_data[index * 4 + 2] + ((uint)float_data[index * 4 + 3] << 16)), 0);

            return NewLine(command, number, setpoint, timeSetpoint, string.Empty);
        }
    }
}
