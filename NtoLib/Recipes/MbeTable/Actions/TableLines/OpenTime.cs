using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class OpenTime : RecipeLine
    {
        public const string ActionName = Commands.OPEN_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public OpenTime(int actionTarget, float timeSetpoint, string comment) : base(ActionName)
        {
            ShutterName = ActionTarget.ShutterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, ShutterName, actionTarget),
                
                new(CellType.Blocked, ""),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatSecond, timeSetpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };
        }
    }
}