using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Open : RecipeLine
    {
        public const string ActionName = Commands.OPEN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Open(int number = 0, string comment = "") : base(ActionName)
        {
            shutterName = GrowthList.ShutterNames[number];
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, shutterName, number),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}