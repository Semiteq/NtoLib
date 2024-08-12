using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class End : RecipeLine
    {
        public const string Name = Commands.END;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public End() : this("Конец рецепта") { }
        public End(string comment) : base(Name)
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