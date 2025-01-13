using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class End : RecipeLine
    {
        public const string ActionName = Commands.END;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public End() : this("Конец рецепта") { }
        public End(string comment) : base(ActionName)
        {
            int actionNumber = Actions[ActionName];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, ActionName, actionNumber),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
        }
    }
}