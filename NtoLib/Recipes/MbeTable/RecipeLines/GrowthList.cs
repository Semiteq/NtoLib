using System;
using System.Collections.Generic;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable
{
    internal abstract class GrowthList : VisualControlBase
    {
        public static TableEnumType ShutterNames { get; private set; }
        public static TableEnumType HeaterNames { get; private set; }
        public static TableEnumType NitrogenSourceNames { get; private set; }

        public static void SetShutterNames(TableEnumType shutterNames)
        {
            ShutterNames = shutterNames ?? throw new ArgumentNullException(nameof(shutterNames));
        }

        public static void SetHeaterNames(TableEnumType heaterNames)
        {
            HeaterNames = heaterNames ?? throw new ArgumentNullException(nameof(heaterNames));
        }

        public static void SetNitrogenSourceNames(TableEnumType nitrogenSourceNames)
        {
            NitrogenSourceNames = nitrogenSourceNames ?? throw new ArgumentNullException(nameof(nitrogenSourceNames));
        }

        /// <summary>
        /// Converts an action name to its corresponding number for writing into a recipe file.
        /// </summary>
        public static int NameToIntConvert(string growthValue, string action)
        {
            var actionType = ActionManager.GetTargetAction(action);
            
            if (actionType == ActionType.Shutter) return ShutterNames[growthValue];
            if (actionType == ActionType.Heater) return HeaterNames[growthValue];
            if (actionType == ActionType.NitrogenSource) return NitrogenSourceNames[growthValue];
            
            throw new KeyNotFoundException("Unknown action type");
        }

        /// <summary>
        /// Returns the action type based on the given number. If not found, returns an empty string.
        /// </summary>
        public static ActionType GetActionType(string number)
        {
            if (ShutterNames.TryGetValue(number, out var value) && value != 0) return ActionType.Shutter;
            if (HeaterNames.TryGetValue(number, out value) && value != 0) return ActionType.Heater;
            if (NitrogenSourceNames.TryGetValue(number, out value) && value != 0) return ActionType.NitrogenSource;
            return ActionType.Unspecified;
        }

        /// <summary>
        /// Returns the lowest shutter number.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the shutter list is empty.</exception>
        public static int GetMinShutter()
        {
            var lowestShutter = ShutterNames.GetLowestNumber();
            if (lowestShutter == -1)
                throw new InvalidOperationException("Shutter list is empty");
            return lowestShutter;
        }

        /// <summary>
        /// Returns the lowest heater number.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the heater list is empty.</exception>
        public static int GetMinHeater()
        {
            var lowestHeater = HeaterNames.GetLowestNumber();
            if (lowestHeater == -1)
                throw new InvalidOperationException("Heater list is empty");
            return lowestHeater;
        }

        /// <summary>
        /// Returns the lowest nitrogen source number.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the nitrogen source list is empty.</exception>
        public static int GetMinNitrogenSource()
        {
            var lowestAmmonia = NitrogenSourceNames.GetLowestNumber();
            if (lowestAmmonia == -1)
                throw new InvalidOperationException("N+ source list is empty");
            return lowestAmmonia;
        }

        /// <summary>
        /// Returns the lowest number based on the action type.
        /// If the action is not shutter, heater, or nitrogen source, returns 0.
        /// </summary>
        public static int GetMinNumber(string action)
        {
            var actionType = ActionManager.GetTargetAction(action);
            return actionType switch
            {
                ActionType.Shutter => GetMinShutter(),
                ActionType.Heater => GetMinHeater(),
                ActionType.NitrogenSource => GetMinNitrogenSource(),
                _ => 0
            };
        }
    }
}
