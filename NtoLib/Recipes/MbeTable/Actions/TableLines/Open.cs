﻿using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Open : RecipeLine
    {
        public const string ActionName = Commands.OPEN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Open(int actionTarget = 0, string comment = "") : base(ActionName)
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
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };
        }
    }
}