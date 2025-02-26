using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.Actions
{
    internal static class ActionManager
    {
        /// <summary>
        /// Returns a dictionary of all commands where the key is the command name and the value is the ID.
        /// </summary>
        public static Dictionary<int, string> Names => new Dictionary<int, string>(ActionEntries.ToDictionary(entry => entry.Id, entry => entry.Command));

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
            var actionEntry = ActionEntries.FirstOrDefault(entry => entry.Command == commandName);
            return actionEntry?.Type ?? ActionType.Unspecified;
        }

        /// <summary>
        /// Returns the action ID based on the command name.
        /// </summary>
        public static int GetActionIdByCommand(string commandName)
        {
            return ActionEntries.FirstOrDefault(entry => entry.Command == commandName)?.Id
                   ?? throw new KeyNotFoundException($"Command '{commandName}' not found.");
        }

        /// <summary>
        /// Returns the command name based on the action ID.
        /// </summary>
        public static string GetActionNameById(int id)
        {
            return ActionEntries.FirstOrDefault(entry => entry.Id == id)?.Command
                   ?? throw new KeyNotFoundException($"ID '{id}' not found.");
        }
        
        /// <summary>
        /// Returns a collection of predefined action entries.
        /// </summary>
        private static IEnumerable<ActionEntry> ActionEntries => new[]
        {
            //              Command                 | ID  |      Type
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
            
            new ActionEntry(Commands.N_RUN,          170, ActionType.NitrogenSource),
            new ActionEntry(Commands.N_CLOSE,        180, ActionType.NitrogenSource),
            new ActionEntry(Commands.N_VENT,         190, ActionType.NitrogenSource)
        };
    }
}
