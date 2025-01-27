using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable
{
    internal static class Actions
    {
        public static TableEnumType Names => new TableEnumType(ActionEntries
            .ToDictionary(entry => entry.Command, entry => entry.Id));

        public static Dictionary<int, string> Types => ActionEntries
            .ToDictionary(entry => entry.Id, entry => entry.Type);

        public static IEnumerable<ActionEntry> ActionEntries => new[]
        {
        new ActionEntry(Commands.CLOSE,            10, "shutter"),
        new ActionEntry(Commands.OPEN,             20, "shutter"),
        new ActionEntry(Commands.OPEN_TIME,        30, "shutter"),
        new ActionEntry(Commands.CLOSE_ALL,        40, "shutter"),
        new ActionEntry(Commands.TEMP,             50, "heater"),
        new ActionEntry(Commands.TEMP_WAIT,        60, "heater"),
        new ActionEntry(Commands.TEMP_BY_SPEED,    70, "heater"),
        new ActionEntry(Commands.TEMP_BY_TIME,     80, "heater"),
        new ActionEntry(Commands.POWER,            90, "heater"),
        new ActionEntry(Commands.POWER_WAIT,      100, "heater"),
        new ActionEntry(Commands.POWER_BY_SPEED,  110, "heater"),
        new ActionEntry(Commands.POWER_BY_TIME,   120, "heater"),
        new ActionEntry(Commands.WAIT,            130, string.Empty),
        new ActionEntry(Commands.FOR,             140, string.Empty),
        new ActionEntry(Commands.END_FOR,         150, string.Empty),
        new ActionEntry(Commands.PAUSE,           160, string.Empty),
        new ActionEntry(Commands.NH3_OPEN,        170, string.Empty),
        new ActionEntry(Commands.NH3_CLOSE,       180, string.Empty),
        new ActionEntry(Commands.NH3_PURGE,       190, string.Empty)
    };

        public class ActionEntry
        {
            public string Command { get; }
            public int Id { get; }
            public string Type { get; }

            public ActionEntry(string command, int id, string type)
            {
                Command = command;
                Id = id;
                Type = type;
            }
        }
    }

}
