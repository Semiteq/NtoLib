using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureWait : RecipeLine
    {
        public const string ActionName = Commands.TEMP_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public TemperatureWait(int actionTarget, float temperatureSetpoint, float timeSetpoint, string comment) : base(
            ActionName)
        {
            HeaterName = ActionTarget.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, HeaterName, actionTarget),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatTemp, temperatureSetpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatSecond, timeSetpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000;
        }
    }
}