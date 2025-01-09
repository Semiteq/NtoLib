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
            int actionNumber = Actions[Name];
            var growthList = new GrowthList();
            _cells = new List<TCell>
            {
                new (CellType._enum, Name, actionNumber),
                new (CellType._int, number),
                new (CellType._blocked, ""),
                new (CellType._blocked, ""),
                new (CellType._blocked, ""),
                new (CellType._string, comment)
            };
        }
    }
}