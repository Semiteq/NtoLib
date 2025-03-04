using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class EndFor_Loop : RecipeLine
    {
        public const string ActionName = Commands.END_FOR;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TimeSpan cycleTime;

        public EndFor_Loop(string comment) : base(ActionName)
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
            cycleTime = TimeSpan.Zero;
        }
    }
}