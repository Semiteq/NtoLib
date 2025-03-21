using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;
using NtoLib.Recipes.MbeTable.Actions.TableLines;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal static class RecipeLineFactory
    {
        private static readonly Dictionary<string, Func<int, float, float, float, float, string, RecipeLine>> Creators = new()
        {
            { Close.ActionName,             (n, _, _, _, _, c) => new Close             (n, c) },
            { Open.ActionName,              (n, _, _, _, _, c) => new Open              (n, c) },
            { OpenTime.ActionName,          (n, _, _, _, t, c) => new OpenTime          (n, t > 0f ? t : 1f, c) },
            { CloseAll.ActionName,          (_, _, _, _, _, c) => new CloseAll          (c) },

            { Temperature.ActionName,       (n, _, s, _, _, c) => new Temperature       (n, s >= 20f ? s : 500f, c) },
            { TemperatureWait.ActionName,   (n, _, s, _, t, c) => new TemperatureWait   (n, s >= 20f ? s : 500f, t > 0f ? t : 60f, c) },
            { TemperatureSmooth.ActionName, (n, i, s, v, t, c) => new TemperatureSmooth (n, i >= 20f ? i : 500f, s >= 20f ? s : 600f, v > 0 ? v : 10f, t > 0f ? t : 600f, c) },

            { Power.ActionName,             (n, _, s, _, _, c) => new Power             (n, s >= 0f ? s : 10f, c) },
            { PowerWait.ActionName,         (n, _, s, _, t, c) => new PowerWait         (n, s >= 0f ? s : 10f, t > 0f ? t : 60f, c) },
            { PowerSmooth.ActionName,       (n, i, s, v, t, c) => new PowerSmooth       (n, i >= 0f ? i : 10f, s >= 0f ? s : 20f, v > 0f ? v : 1f, t > 0f ? t : 600f, c) },

            { Wait.ActionName,              (_, _, _, _, t, c) => new Wait              (t > 0f ? t : 10f, c) },
            { For_Loop.ActionName,          (_, _, s, _, _, c) => new For_Loop          (s > 0 ? (int)s : 5, c) },
            { EndFor_Loop.ActionName,       (_, _, _, _, _, c) => new EndFor_Loop       (c) },
            { Pause.ActionName,             (_, _, _, _, _, c) => new Pause             (c) },

            { N_Run.ActionName,             (n, _, s, _, _, c) => new N_Run             (n, s > 0f ? s : 10f, c) },
            { N_Close.ActionName,           (_, _, _, _, _, c) => new N_Close           (c) },
            { N_Vent.ActionName,            (n, _, s, _, _, c) => new N_Vent            (n, s > 0f ? s : 10f, c) }
        };

        public static RecipeLine NewLine(string actionName, int actionTarget, float setpoint, float initialValue, float speed, float timeSetpoint, string comment)
        {
            return Creators.TryGetValue(actionName, out var creator) ?
                   creator(actionTarget, setpoint, initialValue, speed, timeSetpoint, comment) : null;
        }

        public static RecipeLine NewLine(int[] intData, int[] floatData, int[] boolData, int index)
        {
            var actionName = ActionManager.GetActionNameById(intData[index * 2]);
            var actionTarget = intData[index * 2 + 1];
            var floatValues = new float[4];

            for (var i = 0; i < 4; i++)
            {
                var baseIndex = index * 8 + i * 2;
                uint raw = (uint)floatData[baseIndex] | ((uint)floatData[baseIndex + 1] << 16);
                floatValues[i] = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
            }

            return NewLine(actionName, actionTarget, floatValues[0], floatValues[1], floatValues[2], floatValues[3], string.Empty);
        }


    }
}
