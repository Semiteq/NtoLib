using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Actions
{
    internal class ActionManager
    {
        public static TableEnumType Names => new TableEnumType(ActionEntries
            /// <summary>
            /// Возвращает словарь всех команд, где ключ - название команды, значение - id.
            /// </summary>
            .ToDictionary(entry => entry.Command, entry => entry.Id));

        public static Dictionary<int, ActionType> ActionTypes => ActionEntries
            /// <summary>
            /// Возвращает словарь всех типов действий, где ключ - id, значение - тип действия.
            /// </summary>
            .ToDictionary(entry => entry.Id, entry => entry.Type);

        public static string[] CommandNames => ActionEntries
            /// <summary>
            /// Возвращает список всех коммнад.
            /// </summary>
            .Select(entry => entry.Command)
            .ToArray();

        public static ActionType GetTargetAction(string commandName)
        {
            /// <summary>
            /// Проверка, заслонкам или нагревателям предназначена команда.
            /// Принимает на вход название команды, возвращает тип действия shutter или heater.
            /// </summary>
            var actionEntry = ActionEntries.FirstOrDefault(entry => entry.Command == commandName);
            return actionEntry?.Type ?? ActionType.Unspecified;
        }

        public static int GetActionIdByCommand(string commandName)
        {
            /// <summary>
            /// Возвращает id действия по названию команды.
            /// </summary>
            return ActionEntries.FirstOrDefault(entry => entry.Command == commandName)?.Id
                   ?? throw new KeyNotFoundException($"Command '{commandName}' not found.");
        }


        public static IEnumerable<ActionEntry> ActionEntries => new[]
        {
            //              command                 | id |      type
            //----------------------------------------------------------
            new ActionEntry(Commands.CLOSE,           10, ActionType.Shutter),
            new ActionEntry(Commands.OPEN,            20, ActionType.Shutter),
            new ActionEntry(Commands.OPEN_TIME,       30, ActionType.Shutter),
            new ActionEntry(Commands.CLOSE_ALL,       40, ActionType.Shutter),
            
            new ActionEntry(Commands.TEMP,            50, ActionType.Heater),
            new ActionEntry(Commands.TEMP_WAIT,       60, ActionType.Heater),
            new ActionEntry(Commands.TEMP_BY_SPEED,   70, ActionType.Heater),
            new ActionEntry(Commands.TEMP_BY_TIME,    80, ActionType.Heater),

            new ActionEntry(Commands.POWER,           90, ActionType.Heater),
            new ActionEntry(Commands.POWER_WAIT,     100, ActionType.Heater),
            new ActionEntry(Commands.POWER_BY_SPEED, 110, ActionType.Heater),
            new ActionEntry(Commands.POWER_BY_TIME,  120, ActionType.Heater),

            new ActionEntry(Commands.WAIT,           130, ActionType.Unspecified),
            new ActionEntry(Commands.FOR,            140, ActionType.Unspecified),
            new ActionEntry(Commands.END_FOR,        150, ActionType.Unspecified),
            new ActionEntry(Commands.PAUSE,          160, ActionType.Unspecified),
            
            new ActionEntry(Commands.NH3_OPEN,       170, ActionType.Unspecified),
            new ActionEntry(Commands.NH3_CLOSE,      180, ActionType.Unspecified),
            new ActionEntry(Commands.NH3_PURGE,      190, ActionType.Unspecified)
        };
    }
}
