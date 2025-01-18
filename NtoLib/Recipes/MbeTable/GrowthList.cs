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
            // Проверка, заслонкам или нагревателям предназначена команда
            return Params.ActionTypes[RecipeLine.Actions[currentAction]];
        }

        public static int NameToIntConvert(string growthValue, string action)
        {
            //Конвертация названия заслонки/нагревателя в номер для записи в файл рецепта
            string targetAction = GetTargetAction(action);
            
            if (targetAction == "shutter")
                return ShutterNames[growthValue];
            
            else if (targetAction == "heater")
                return HeaterNames[growthValue];

            else
                throw new KeyNotFoundException("Неизвестный тип действия");
        }

        public static int GetMinShutter()
        {
            return ShutterNames.GetLowestNumber();
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
