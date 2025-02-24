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
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);

            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
            cycleTime = TimeSpan.Zero;
        }
    }
}
