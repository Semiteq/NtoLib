using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class NH3_Open : RecipeLine
    {
        public const string Name = Commands.NH3_OPEN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public NH3_Open() : this("") { }

        public NH3_Open(string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

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