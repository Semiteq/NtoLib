using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class NH3_Purge : RecipeLine
    {
        public const string Name = Commands.NH3_PURGE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public NH3_Purge() : this("") { }
        public NH3_Purge(string comment) : base(Name)
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