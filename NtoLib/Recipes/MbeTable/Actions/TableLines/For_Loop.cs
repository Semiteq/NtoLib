using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class For_Loop : RecipeLine
    {
        public const string ActionName = Commands.FOR;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public For_Loop(int setpoint, string comment) : base(ActionName)
        {
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Blocked, ""),
                
                new(CellType.Int, setpoint),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 1;
            MaxSetpoint = 100f;
        }
    }
}