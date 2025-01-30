using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class NH3_Purge : RecipeLine
    {
        public const string ActionName = Commands.NH3_PURGE;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public NH3_Purge() : this("") { }
        public NH3_Purge(string comment) : base(ActionName)
        {
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);

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