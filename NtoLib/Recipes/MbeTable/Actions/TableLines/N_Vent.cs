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
            NitrogenSourceName = ActionTarget.NitrogenSourceNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, NitrogenSourceName, actionTarget),
                
                new(CellType.Blocked, ""),
                new(CellType.FloatSccm, setpoint),
                
                new(CellType.Blocked, ""),
                new(CellType.Blocked, ""),
                
                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };
            
            MinSetpoint = 0.1f;
            MaxSetpoint = 1000f;
        }
    }
}