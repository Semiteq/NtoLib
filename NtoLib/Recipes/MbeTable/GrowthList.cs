using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable
{
    internal class GrowthList
    {
        //todo: static?
        public static TableEnumType ShutterNames { get; private set; }
        public static TableEnumType HeaterNames { get; private set; }

        public GrowthList()
        {
            MbeTableFB table = new();

            //ShutterNames = FillDictNames(table, Params.FirstPinShutterName, Params.ShutterNameQuantity);
            ShutterNames = new()
            {
                { "Shut1", 1 },
                { "Shut2", 2 }
            };

            HeaterNames = new()
            {
                { "Heat1", 3 },
                { "Heat2", 4 }
            };
            //HeaterNames = FillDictNames(table, Params.FirstPinHeaterName, Params.HeaterNameQuantity);
        }

        public static string GetTargetAction(string currentAction)
        {
            /// <summary>
            /// Проверка, заслонкам или нагревателям предназначена команда.
            /// Принимает на вход название команды, возвращает тип действия shutter или heater.
            /// </summary>
            return Params.ActionTypes[RecipeLine.Actions[currentAction]];
        }

        public static int NameToIntConvert(string growthValue, string action)
        {
            /// <summary>
            /// Конвертация названия заслонки/нагревателя в номер для записи в файл рецепта
            /// </summary>
            string targetAction = GetTargetAction(action);

            if (targetAction == "shutter")
                return ShutterNames[growthValue];

            else if (targetAction == "heater")
                return HeaterNames[growthValue];

            else
                throw new KeyNotFoundException("Неизвестный тип действия");
        }

        public static string GetActionType(string number)
        {

            /// <summary>
            /// Возвращает тип действия по номеру заслонки/нагревателя. В случае отсутствия возвращает пустую строку.
            /// </summary>
            if (ShutterNames[number] != 0)
                return "shutter";
            else if (HeaterNames[number] != 0)
                return "heater";
            return string.Empty;
        }

        public static int GetMinShutter()
        {
            /// <summary>
            /// Возвращает минимальный номер заслонки
            /// </summary>
            return ShutterNames.GetLowestNumber();
        }

        public static int GetMinHeater()
        {
            /// <summary>
            /// Возвращает минимальный номер нагревателя
            /// </summary>
            return HeaterNames.GetLowestNumber();
        }

        public static int GetMinNumber(string action)
        {
            /// <summary> 
            /// Возвращает минимальный номер заслонки/нагревателя в зависимости от типа действия. 
            /// Принимает на вход действие.
            /// </summary>
            string targetAction = GetTargetAction(action);
            if (targetAction == "shutter")
                return GetMinShutter();
            else if (targetAction == "heater")
                return GetMinHeater();
            else
                throw new KeyNotFoundException("Неизвестный тип действия");
        }

        private TableEnumType FillDictNames(MbeTableFB table, int startBit, int quantity)
        {
            TableEnumType names = new();

            for (int i = 0; i < quantity; i++)
            {
                var pinString = table.GetPinString(i + startBit);
                if (pinString is not null)
                {
                    names.Add(pinString.ToString(), i);
                }
                else
                {
                    continue;
                }
            }
            return names;
        }
    }
}
