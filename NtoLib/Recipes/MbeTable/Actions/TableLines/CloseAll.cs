using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class CloseAll : RecipeLine
    {
        public const string ActionName = Commands.CLOSE_ALL;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public CloseAll(string comment) : base(ActionName)
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