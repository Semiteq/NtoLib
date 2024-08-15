using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class CloseAll : RecipeLine
    {
        public const string Name = Commands.CLOSE_ALL;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public CloseAll() : this("") { }

        public CloseAll(string comment) : base(Name)
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