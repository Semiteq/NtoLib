using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class N_Run : RecipeLine
    {
        public const string ActionName = Commands.N_RUN;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public N_Run(int number, float setpoint, string comment) : base(ActionName)
        {
            nitrogenSourceName = GrowthList.NitrogenSourceNames[number];
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            _cells = new List<TCell>
            {
                new TCell(CellType._enum, ActionName, actionNumber),
                new TCell(CellType._enum, nitrogenSourceName, number),
                new TCell(CellType._float, setpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
        }
    }
}