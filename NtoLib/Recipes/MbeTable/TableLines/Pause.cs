using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Pause : RecipeLine
    {
        public const string ActionName = Commands.PAUSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Pause(string comment = "") : base(ActionName)
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