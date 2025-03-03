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
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._blocked, ""),
                new(CellType._int, setpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 1;
            MaxSetpoint = 100f;
        }
    }
}