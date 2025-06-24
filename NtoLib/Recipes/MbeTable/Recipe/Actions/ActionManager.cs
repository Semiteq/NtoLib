using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Recipe.Actions
{
    public class ActionManager
    {
        public readonly ActionEntry Close = new(10, "CLOSE", ActionType.Shutter);
        public readonly ActionEntry Open = new(20, "OPEN", ActionType.Shutter);
        public readonly ActionEntry OpenTime = new(30, "OPEN TIME", ActionType.Shutter);
        public readonly ActionEntry CloseAll = new(40, "CLOSE ALL", ActionType.Shutter);

        public readonly ActionEntry Temperature = new(50, "TEMP", ActionType.Heater);
        public readonly ActionEntry TemperatureWait = new(60, "TEMP + WAIT", ActionType.Heater);
        public readonly ActionEntry TemperatureSmooth = new(70, "TEMP SMOOTH", ActionType.Heater);

        public readonly ActionEntry Power = new(80, "POWER", ActionType.Heater);
        public readonly ActionEntry PowerWait = new(90, "POWER + WAIT", ActionType.Heater);
        public readonly ActionEntry PowerSmooth = new(100, "POWER SMOOTH", ActionType.Heater);

        public readonly ActionEntry Wait = new(110, "WAIT", ActionType.Service);
        public readonly ActionEntry ForLoop = new(120, "FOR", ActionType.Service);
        public readonly ActionEntry EndForLoop = new(130, "END FOR", ActionType.Service);
        public readonly ActionEntry Pause = new(140, "PAUSE", ActionType.Service);

        public readonly ActionEntry NRun = new(150, "N+ RUN", ActionType.NitrogenSource);
        public readonly ActionEntry NClose = new(160, "N+ CLOSE", ActionType.NitrogenSource);
        public readonly ActionEntry NVent = new(170, "N+ VENT", ActionType.NitrogenSource);

        private readonly List<ActionEntry> Actions;

        public ActionManager()
        {
            Actions = new List<ActionEntry>
            {
                Close, Open, OpenTime, CloseAll,
                Temperature, TemperatureWait, TemperatureSmooth,
                Power, PowerWait, PowerSmooth,
                Wait, ForLoop, EndForLoop, Pause,
                NRun, NClose, NVent
            };
        }

        public ActionEntry GetActionEntryById(int id)
        {
            return Actions.FirstOrDefault(action => action.Id == id)
                   ?? throw new KeyNotFoundException($"Action with ID {id} not found.");
        }
    }


}