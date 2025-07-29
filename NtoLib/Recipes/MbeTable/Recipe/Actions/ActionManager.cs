using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.Recipe.Actions
{
    public class ActionManager
    {
        public readonly ActionEntry Close = new(10, "CLOSE", ActionType.Shutter, DeployDuration.Immediately);
        public readonly ActionEntry Open = new(20, "OPEN", ActionType.Shutter, DeployDuration.Immediately);
        public readonly ActionEntry OpenTime = new(30, "OPEN TIME", ActionType.Shutter, DeployDuration.TimeSetpoint);
        public readonly ActionEntry CloseAll = new(40, "CLOSE ALL", ActionType.Shutter, DeployDuration.Immediately);

        public readonly ActionEntry Temperature = new(50, "TEMP", ActionType.Heater, DeployDuration.Immediately);
        public readonly ActionEntry TemperatureWait = new(60, "TEMP + WAIT", ActionType.Heater, DeployDuration.TimeSetpoint);
        public readonly ActionEntry TemperatureSmooth = new(70, "TEMP SMOOTH", ActionType.Heater, DeployDuration.TimeSetpoint);

        public readonly ActionEntry Power = new(80, "POWER", ActionType.Heater, DeployDuration.Immediately);
        public readonly ActionEntry PowerWait = new(90, "POWER + WAIT", ActionType.Heater, DeployDuration.TimeSetpoint);
        public readonly ActionEntry PowerSmooth = new(100, "POWER SMOOTH", ActionType.Heater, DeployDuration.TimeSetpoint);

        public readonly ActionEntry Wait = new(110, "WAIT", ActionType.Service, DeployDuration.TimeSetpoint);
        public readonly ActionEntry ForLoop = new(120, "FOR", ActionType.Service, DeployDuration.Immediately);
        public readonly ActionEntry EndForLoop = new(130, "END FOR", ActionType.Service, DeployDuration.Immediately);
        public readonly ActionEntry Pause = new(140, "PAUSE", ActionType.Service, DeployDuration.TimeSetpoint);

        public readonly ActionEntry NRun = new(150, "N+ RUN", ActionType.NitrogenSource, DeployDuration.Immediately);
        public readonly ActionEntry NClose = new(160, "N+ CLOSE", ActionType.NitrogenSource, DeployDuration.Immediately);
        public readonly ActionEntry NVent = new(170, "N+ VENT", ActionType.NitrogenSource, DeployDuration.TimeSetpoint);

        private readonly List<ActionEntry> _actions;

        public ActionManager()
        {
            _actions = new List<ActionEntry>
            {
                Close, Open, OpenTime, CloseAll,
                Temperature, TemperatureWait, TemperatureSmooth,
                Power, PowerWait, PowerSmooth,
                Wait, ForLoop, EndForLoop, Pause,
                NRun, NClose, NVent
            };
        }

        public bool GetActionEntryById(int id, out ActionEntry actionEntry, out string errorString)
        {
            actionEntry = _actions.FirstOrDefault(action => action.Id == id);
            if (actionEntry != null)
            {
                errorString = string.Empty;
                return true;
            }

            errorString = $"Action with ID {id} not found.";
            return false;
        }
    }
}