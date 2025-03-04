using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Wait : RecipeLine
    {
        public const string ActionName = Commands.WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public Wait(float timeSetpoint, string comment) : base(ActionName)
        {
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Blocked, ""),
                
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