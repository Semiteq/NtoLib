using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Close : RecipeLine
    {
        public const string Name = Commands.CLOSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Close() : this(1, "") { }

        public Close(int number, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];
            _cells = new List<TCell>
            {
                new(CellType._enum, Name, actionNumber),
                new(CellType._int, number),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}