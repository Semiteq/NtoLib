using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class NH3_Close : RecipeLine
    {
        public const string Name = Commands.NH3_CLOSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public NH3_Close() : this("") { }

        public NH3_Close(string comment) : base(Name)
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
        }
    }
}