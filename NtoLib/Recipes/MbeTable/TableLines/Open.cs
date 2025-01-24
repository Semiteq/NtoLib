﻿using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Open : RecipeLine
    {
        public const string ActionName = Commands.OPEN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Open(int number = 0, string comment = "") : base(ActionName)
        {
            shutterName = GrowthList.Instance.ShutterNames[number];
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, shutterName, number),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}