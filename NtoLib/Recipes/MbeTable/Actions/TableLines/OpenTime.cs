﻿using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class OpenTime : RecipeLine
    {
        public const string ActionName = Commands.OPEN_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public OpenTime(int number, float timeSetpoint, string comment) : base(ActionName)
        {
            ShutterName = GrowthList.ShutterNames.FirstOrDefault(x => x.Key == number).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, ShutterName, number),
                new(CellType._blocked, ""),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}