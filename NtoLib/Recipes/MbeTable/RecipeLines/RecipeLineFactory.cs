using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal class RecipeLineFactory
    {
        private readonly Dictionary<string, Func<int, float, float, string, RecipeLine>> _creators = new()
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

            { N_Run.ActionName,           (n, s, _, c) => new N_Run           (n, s > 0f ? s : 10f, c) },
            { N_Close.ActionName,          (_, _, _, c) => new N_Close          (c) },
            { N_Vent.ActionName,          (n, s, _, c) => new N_Vent          (n, s > 0f ? s : 10f, c) }
        };

        public RecipeLine NewLine(string command, int number, float setpoint, float timeSetpoint, string comment)
        {
            return _creators.TryGetValue(command, out var creator) ?
                   creator(number, setpoint, timeSetpoint, comment) : null;
        }

        public RecipeLine NewLine(ushort[] intData, ushort[] floatData, ushort[] boolData, int index)
        {
            var number = (int)intData[index * 2 + 1];
            var command = ActionManager.Names.FirstOrDefault(x => x.Key == number).Value.ToString();
            
            var setpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)floatData[index * 4] + ((uint)floatData[index * 4 + 1] << 16)), 0);
            var timeSetpoint = BitConverter.ToSingle(BitConverter.GetBytes((uint)floatData[index * 4 + 2] + ((uint)floatData[index * 4 + 3] << 16)), 0);

            return NewLine(command, number, setpoint, timeSetpoint, string.Empty);
        }
    }
}
