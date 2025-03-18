using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class PowerWait : RecipeLine
    {
        public const string ActionName = Commands.POWER_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public PowerWait(int actionTarget, float powerSetpoint, float timeSetpoint, string comment) : base(ActionName)
        {
            HeaterName = ActionTarget.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, HeaterName, actionTarget),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatPercent, powerSetpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatSecond, timeSetpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}