using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Pause : RecipeLine
    {
        public const string ActionName = Commands.PAUSE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Pause(string comment) : base(ActionName)
        {
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);

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