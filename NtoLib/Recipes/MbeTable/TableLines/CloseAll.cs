using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class CloseAll : RecipeLine
    {
        public const string ActionName = Commands.CLOSE_ALL;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public CloseAll(string comment = "") : base(ActionName)
        {
            int actionNumber = Actions.Names[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}