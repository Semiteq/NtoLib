using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal static class RecipeLineFactory
    {
        private static readonly Dictionary<string, Func<int, float, float, float, float, string, RecipeLine>> _creators = new()
        {
            { Close.ActionName,             (n, _, _, _, _, c) => new Close             (n, c) },
            { Open.ActionName,              (n, _, _, _, _, c) => new Open              (n, c) },
            { OpenTime.ActionName,          (n, _, _, _, t, c) => new OpenTime          (n, t > 0f ? t : 1f, c) },
            { CloseAll.ActionName,          (_, _, _, _, _, c) => new CloseAll          (c) },

            { Temperature.ActionName,       (n, s, _, _, _, c) => new Temperature       (n, s > 0f ? s : 500f, c) },
            { TemperatureWait.ActionName,   (n, s, _, _, t, c) => new TemperatureWait   (n, s > 0f ? s : 500f, t > 0f ? t : 60f, c) },
            { TemperatureSmooth.ActionName, (n, s, i, v, t, c) => new TemperatureSmooth (n, s > 0f ? s : 500f, i > 0f ? s : 600, v > 0 ? v : 10f, t > 0f ? t : 600f, c) },

            { Power.ActionName,             (n, s, _, _, _, c) => new Power             (n, s > 0f ? s : 10f, c) },
            { PowerWait.ActionName,         (n, s, _, _, t, c) => new PowerWait         (n, s > 0f ? s : 10f, t > 0f ? t : 60f, c) },
            { PowerSmooth.ActionName,       (n, s, i, v, t, c) => new PowerSmooth       (n, s > 0f ? s : 10f, i > 0f ? s : 20, v > 0f ? v : 1f, t > 0f ? t : 120f, c) },

            { Wait.ActionName,              (_, _, _, _, t, c) => new Wait              (t > 0f ? t : 10f, c) },
            { For_Loop.ActionName,          (n, _, _, _, _, c) => new For_Loop          (n > 0 ? n : 5, c) },
            { EndFor_Loop.ActionName,       (_, _, _, _, _, c) => new EndFor_Loop       (c) },
            { Pause.ActionName,             (_, _, _, _, _, c) => new Pause             (c) },

            { N_Run.ActionName,             (n, s, _, _, _, c) => new N_Run             (n, s > 0f ? s : 10f, c) },
            { N_Close.ActionName,           (_, _, _, _, _, c) => new N_Close           (c) },
            { N_Vent.ActionName,            (n, s, _, _, _, c) => new N_Vent            (n, s > 0f ? s : 10f, c) }
        };

        public static RecipeLine NewLine(string actionName, int actionTarget, float setpoint, float initialValue, float speed, float timeSetpoint, string comment)
        {
            return _creators.TryGetValue(actionName, out var creator) ?
                   creator(actionTarget, setpoint, initialValue, speed, timeSetpoint, comment) : null;
        }

        public static RecipeLine NewLine(ushort[] intData, ushort[] floatData, ushort[] boolData, int index)
        {
            var actionName = ActionManager.GetActionNameById(intData[index * 2]).ToString();
            var actionTarget = intData[index * 2 + 1];
            var setpoint = BitConverter.ToSingle(BitConverter.GetBytes(floatData[index * 4] + ((uint)floatData[index * 4 + 1] << 16)), 0);
            var initialValue = BitConverter.ToSingle(BitConverter.GetBytes(floatData[index * 4 + 2] + ((uint)floatData[index * 4 + 3] << 16)), 0);
            var speed = BitConverter.ToSingle(BitConverter.GetBytes(floatData[index * 4 + 4] + ((uint)floatData[index * 4 + 5] << 16)), 0);
            var timeSetpoint = BitConverter.ToSingle(BitConverter.GetBytes(floatData[index * 4 + 6] + ((uint)floatData[index * 4 + 7] << 16)), 0);

            return NewLine(actionName, actionTarget, setpoint, initialValue, speed, timeSetpoint, string.Empty);
        }
    }
}
