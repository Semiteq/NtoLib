using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class N_Close : RecipeLine
    {
        public const string ActionName = Commands.N_CLOSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public N_Close(string comment) : base(ActionName)
        {
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };
        }
    }
}