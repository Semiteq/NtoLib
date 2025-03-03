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
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, actionTarget),
                new(CellType._floatPercent, powerSetpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}