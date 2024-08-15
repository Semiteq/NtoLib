using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class EndFor_Loop : RecipeLine
    {
        public const string Name = Commands.END_FOR;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TimeSpan cycleTime;

        public EndFor_Loop() : this("") { }

        public EndFor_Loop(string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
            cycleTime = TimeSpan.Zero;
        }
    }
}
