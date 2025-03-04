using System;
using System.Collections.Generic;
using System.Linq;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    internal abstract class ActionTarget : VisualControlBase
    {
        public static Dictionary<int, string> ShutterNames { get; private set; } = new();
        public static Dictionary<int, string> HeaterNames { get; private set; } = new();
        public static Dictionary<int, string> NitrogenSourceNames { get; private set; } = new();

        /// <summary>
        /// Sets the names for the given action type.
        /// </summary>
        public static void SetNames(ActionType type, Dictionary<int, string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            switch (type)
            {
                case ActionType.Shutter:
                    ShutterNames = new Dictionary<int, string>(names);
                    break;
                case ActionType.Heater:
                    HeaterNames = new Dictionary<int, string>(names);
                    break;
                case ActionType.NitrogenSource:
                    NitrogenSourceNames = new Dictionary<int, string>(names);
                    break;
                default:
                    throw new ArgumentException($"Unsupported action type: {type}", nameof(type));
            }
        }

        /// <summary>
        /// Converts an action name to its corresponding number for writing into a recipe file.
        /// </summary>
        public static int GetActionTypeByName(string growthValue, string action)
        {
            var actionType = ActionManager.GetTargetAction(action);

            return actionType switch
            {
                ActionType.Shutter => ShutterNames.FirstOrDefault(x => x.Value == growthValue).Key,
                ActionType.Heater => HeaterNames.FirstOrDefault(x => x.Value == growthValue).Key,
                ActionType.NitrogenSource => NitrogenSourceNames.FirstOrDefault(x => x.Value == growthValue).Key,
                _ => throw new KeyNotFoundException("Unknown action type")
            };
        }

        /// <summary>
        /// Returns the lowest number based on the action type.
        /// If the action is not shutter, heater, or nitrogen source, returns 0.
        /// </summary>
        public static int GetMinNumber(string action)
        {
            ActionType actionType = ActionManager.GetTargetAction(action);
            return actionType switch
            {
                ActionType.Shutter => ShutterNames.Keys.DefaultIfEmpty(0).Min(),
                ActionType.Heater => HeaterNames.Keys.DefaultIfEmpty(0).Min(),
                ActionType.NitrogenSource => NitrogenSourceNames.Keys.DefaultIfEmpty(0).Min(),
                _ => 0
            };
        }
    }
}
