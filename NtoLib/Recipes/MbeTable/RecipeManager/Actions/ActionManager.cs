using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;

namespace NtoLib.Recipes.MbeTable.RecipeManager.Actions
{
    public class ActionManager
    {
        public readonly ActionEntry Close = new(10, "CLOSE", ActionType.Shutter, DeployDuration.Immediate);
        public readonly ActionEntry Open = new(20, "OPEN", ActionType.Shutter, DeployDuration.Immediate);
        public readonly ActionEntry OpenTime = new(30, "OPEN TIME", ActionType.Shutter, DeployDuration.LongLasting);
        public readonly ActionEntry CloseAll = new(40, "CLOSE ALL", ActionType.Shutter, DeployDuration.Immediate);

        public readonly ActionEntry Temperature = new(50, "TEMP", ActionType.Heater, DeployDuration.Immediate);
        public readonly ActionEntry TemperatureWait = new(60, "TEMP + WAIT", ActionType.Heater, DeployDuration.LongLasting);
        public readonly ActionEntry TemperatureSmooth = new(70, "TEMP SMOOTH", ActionType.Heater, DeployDuration.LongLasting);

        public readonly ActionEntry Power = new(80, "POWER", ActionType.Heater, DeployDuration.Immediate);
        public readonly ActionEntry PowerWait = new(90, "POWER + WAIT", ActionType.Heater, DeployDuration.LongLasting);
        public readonly ActionEntry PowerSmooth = new(100, "POWER SMOOTH", ActionType.Heater, DeployDuration.LongLasting);

        public readonly ActionEntry Wait = new(110, "WAIT", ActionType.Service, DeployDuration.LongLasting);
        public readonly ActionEntry ForLoop = new(120, "FOR", ActionType.Service, DeployDuration.Immediate);
        public readonly ActionEntry EndForLoop = new(130, "END FOR", ActionType.Service, DeployDuration.Immediate);
        public readonly ActionEntry Pause = new(140, "PAUSE", ActionType.Service, DeployDuration.LongLasting);

        public readonly ActionEntry NRun = new(150, "N+ RUN", ActionType.NitrogenSource, DeployDuration.Immediate);
        public readonly ActionEntry NClose = new(160, "N+ CLOSE", ActionType.NitrogenSource, DeployDuration.Immediate);
        public readonly ActionEntry NVent = new(170, "N+ VENT", ActionType.NitrogenSource, DeployDuration.LongLasting);

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
        
        public Dictionary<int, string> GetAllActions()
        {
            var actionList = _actions?.ToDictionary(a => a.Id, a => a.Name);

            if (actionList == null || !actionList.Any())
                throw new InvalidOperationException("Failed to retrieve action list.");
    
            return actionList;
        }

        public ActionEntry GetActionEntryById(int id)
        {
            var action = _actions.FirstOrDefault(a => a.Id == id);
            return action ?? throw new KeyNotFoundException($"Action with ID {id} not found.");
        }
        
        public ActionType GetActionTypeById(int id)
        {
            var action = _actions.FirstOrDefault(a => a.Id == id);
            return action?.ActionType ?? throw new KeyNotFoundException($"Action with ID {id} not found.");
        }
    }
}