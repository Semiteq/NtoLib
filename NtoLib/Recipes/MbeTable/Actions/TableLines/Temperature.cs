using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Temperature : RecipeLine
    {
        public const string ActionName = Commands.TEMP;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Temperature(int actionTarget, float temperatureSetpoint, string comment) : base(ActionName)
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
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000;
        }
    }
}