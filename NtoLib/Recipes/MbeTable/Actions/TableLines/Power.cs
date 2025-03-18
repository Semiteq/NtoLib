using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Power : RecipeLine
    {
        public const string ActionName = Commands.POWER;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Power(int actionTarget, float powerSetpoint, string comment) : base(ActionName)
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
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}