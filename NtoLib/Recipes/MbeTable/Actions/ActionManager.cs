using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Actions
{
    internal static class ActionManager
    {
        /// <summary>
        /// Returns a dictionary of all commands where the key is the command name and the value is the ID.
        /// </summary>
        public static Dictionary<int, string> Names => new(ActionEntries.ToDictionary(entry => entry.Id, entry => entry.Command));

        /// <summary>
        /// Returns a dictionary of all action types where the key is the ID and the value is the action type.
        /// </summary>
        public static Dictionary<int, ActionType> ActionTypes => ActionEntries.ToDictionary(entry => entry.Id, entry => entry.Type);

        /// <summary>
        /// Returns a list of all command names.
        /// </summary>
        public static string[] CommandNames => ActionEntries.Select(entry => entry.Command).ToArray();

        /// <summary>
        /// Determines whether the command is intended for .
        /// Takes a command name as input and returns the action type.
        /// </summary>
        public static ActionType GetTargetAction(string commandName)
        {
            ActionEntry actionEntry = ActionEntries.FirstOrDefault(entry => entry.Command == commandName);
            return actionEntry?.Type ?? ActionType.Unspecified;
        }

        /// <summary>
        /// Returns the action ID based on the command name.
        /// </summary>
        public static int GetActionIdByCommand(string commandName)
        {
            return ActionEntries.FirstOrDefault(entry => entry.Command == commandName)?.Id
                   ?? throw new KeyNotFoundException($"Команда: '{commandName}' не найдена.");
        }

        /// <summary>
        /// Returns the command name based on the action ID.
        /// </summary>
        public static string GetActionNameById(int id)
        {
            return ActionEntries.FirstOrDefault(entry => entry.Id == id)?.Command
                   ?? throw new KeyNotFoundException($"Действие с id: '{id}' не найдено.");
        }

        /// <summary>
        /// Returns a collection of predefined action entries.
        /// </summary>
        private static IEnumerable<ActionEntry> ActionEntries => new[]
        {
            //              Command               | ID  |      Type
            //---------------------------------------------------------
            new ActionEntry(Commands.CLOSE,         10, ActionType.Shutter),
            new ActionEntry(Commands.OPEN,          20, ActionType.Shutter),
            new ActionEntry(Commands.OPEN_TIME,     30, ActionType.Shutter),
            new ActionEntry(Commands.CLOSE_ALL,     40, ActionType.Shutter),

            new ActionEntry(Commands.TEMP,          50, ActionType.Heater),
            new ActionEntry(Commands.TEMP_WAIT,     60, ActionType.Heater),
            new ActionEntry(Commands.TEMP_SMOOTH,   70, ActionType.Heater),

            new ActionEntry(Commands.POWER,         80, ActionType.Heater),
            new ActionEntry(Commands.POWER_WAIT,    90, ActionType.Heater),
            new ActionEntry(Commands.POWER_SMOOTH,  100, ActionType.Heater),


            new ActionEntry(Commands.WAIT,          110, ActionType.Unspecified),
            new ActionEntry(Commands.FOR,           120, ActionType.Unspecified),
            new ActionEntry(Commands.END_FOR,       130, ActionType.Unspecified),
            new ActionEntry(Commands.PAUSE,         140, ActionType.Unspecified),

            new ActionEntry(Commands.N_RUN,         150, ActionType.NitrogenSource),
            new ActionEntry(Commands.N_CLOSE,       160, ActionType.NitrogenSource),
            new ActionEntry(Commands.N_VENT,        170, ActionType.NitrogenSource)
        };
    }
}
