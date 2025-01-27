using System;
using System.Collections.Generic;
using FB.VisualFB;

namespace NtoLib.Recipes.MbeTable
{
    internal sealed class GrowthList : VisualControlBase
    {
        private static readonly GrowthList instance = new();
        TableControl tableControl = new();
        private GrowthList()
        {
           // // debug
           // ShutterNames = new()
           //{
           //     { "Shut1", 1 },
           //     { "Shut2", 2 }
           //};

           // HeaterNames = new()
           // {
           //     { "Heat1", 3 },
           //     { "Heat2", 4 }
           // };
        }

        public static GrowthList Instance => instance;

        public TableEnumType ShutterNames { get; private set; }
        public TableEnumType HeaterNames { get; private set; }

        public void SetShutterNames(TableEnumType shutterNames) 
        {
            if (shutterNames == null)
                throw new ArgumentNullException(nameof(shutterNames));
            ShutterNames = shutterNames;    
        }
        public void SetHeaterNames(TableEnumType heaterNames)
        {
            if (heaterNames == null)
                throw new ArgumentNullException(nameof(heaterNames));
            HeaterNames = heaterNames;
        }

        public string GetTargetAction(string currentAction)
        {
            /// <summary>
            /// Проверка, заслонкам или нагревателям предназначена команда.
            /// Принимает на вход название команды, возвращает тип действия shutter или heater.
            /// </summary>
            return Actions.Types[Actions.Names[currentAction]];
        }

        public int NameToIntConvert(string growthValue, string action)
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

        public string GetActionType(string number)
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

        public int GetMinShutter()
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

        public int GetMinHeater()
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

        public int GetMinNumber(string action)
        {
            /// <summary> 
            /// Возвращает минимальный номер заслонки/нагревателя в зависимости от типа действия. 
            /// Принимает на вход действие. Если не shutter и не heater, то возвращает 0.
            /// </summary>
            string targetAction = GetTargetAction(action);
            if (targetAction == "shutter")
                return GetMinShutter();
            else if (targetAction == "heater")
                return GetMinHeater();
            else
                return 0;
        }
    }
}
