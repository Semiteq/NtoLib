using System;
using System.Collections.Generic;
using FB.VisualFB;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable
{
    
    internal sealed class GrowthList : VisualControlBase
    {
        private static readonly GrowthList instance = new();
        private GrowthList() { }

        public static GrowthList Instance => instance;

        public static TableEnumType ShutterNames { get; private set; }
        public static TableEnumType HeaterNames { get; private set; }

        public static void SetShutterNames(TableEnumType shutterNames)
        {
            if (shutterNames == null)
                throw new ArgumentNullException(nameof(shutterNames));
            ShutterNames = shutterNames;
        }
        public static void SetHeaterNames(TableEnumType heaterNames)
        {
            if (heaterNames == null)
                throw new ArgumentNullException(nameof(heaterNames));
            HeaterNames = heaterNames;
        }

        public static int NameToIntConvert(string growthValue, string action)
        {
            /// <summary>
            /// Конвертация названия заслонки/нагревателя в номер для записи в файл рецепта
            /// </summary>
            if (ActionManager.GetTargetAction(action) == ActionType.Shutter)
                return ShutterNames[growthValue];

            else if (ActionManager.GetTargetAction(action) == ActionType.Heater)
                return HeaterNames[growthValue];

            else
                throw new KeyNotFoundException("Неизвестный тип действия");
        }

        public static string GetActionType(string number)
        {

            /// <summary>
            /// Возвращает тип действия по номеру заслонки/нагревателя. В случае отсутствия возвращает пустую строку.
            /// </summary>

            try
            {
                if (ShutterNames[number] != 0)
                    return "shutter";
            }
            catch (KeyNotFoundException) { }

            try
            {
                if (HeaterNames[number] != 0)
                    return "heater";
            }
            catch (KeyNotFoundException) { }

            return string.Empty;
        }

        public static int GetMinShutter()
        {
            /// <summary>
            /// Возвращает минимальный номер заслонки
            /// </summary>
            /// <exception cref="InvalidOperationException">Выбрасывается, если в список заслонок пуст.</exception>

            int lowestShutter = ShutterNames.GetLowestNumber();

            if (lowestShutter == -1)
            {
                throw new InvalidOperationException("Список заслонок пуст");
            }
            return lowestShutter;
        }

        public static int GetMinHeater()
        {
            /// <summary>
            /// Возвращает минимальный номер нагревателя
            /// </summary>
            /// <exception cref="InvalidOperationException">Выбрасывается, если в список нагревателей пуст.</exception>
            int lowestHeater = HeaterNames.GetLowestNumber();

            if (lowestHeater == -1)
            {
                throw new InvalidOperationException("Список нагревателей пуст");
            }
            return lowestHeater;
        }

        public static int GetMinNumber(string action)
        {
            /// <summary> 
            /// Возвращает минимальный номер заслонки/нагревателя в зависимости от типа действия. 
            /// Принимает на вход действие. Если не shutter и не heater, то возвращает 0.
            /// </summary>
            if (ActionManager.GetTargetAction(action) == ActionType.Shutter)
                return GetMinShutter();
            else if (ActionManager.GetTargetAction(action) == ActionType.Heater)
                return GetMinHeater();
            else
                return 0;
        }
    }
}
