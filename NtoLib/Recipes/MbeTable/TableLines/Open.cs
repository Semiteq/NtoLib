using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Open : RecipeLine
    {
        public const string Name = Commands.OPEN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Open() : this(1, "") { }

        public Open(int number, string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
        }
    }
}