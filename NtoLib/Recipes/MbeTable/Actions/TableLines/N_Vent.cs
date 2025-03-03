using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class N_Vent : RecipeLine
    {
        public const string ActionName = Commands.N_VENT;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public N_Vent(int actionTarget, float setpoint, string comment) : base(ActionName)
        {
            NitrogenSourceName = GrowthList.NitrogenSourceNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, NitrogenSourceName, actionTarget),
                new(CellType._float, setpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}