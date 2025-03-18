using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class PowerSmooth : RecipeLine
    {
        public const string ActionName = Commands.POWER_SMOOTH;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerSmooth(int actionTarget, float initialValue, float powerSetpoint, float speed, float timeSetpoint, string comment) : base(ActionName)
        {
            HeaterName = ActionTarget.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, HeaterName, actionTarget),

                new(CellType.FloatPercent, initialValue),
                new(CellType.FloatPercent, powerSetpoint),

                new(CellType.FloatPowerSpeed, speed),
                new(CellType.FloatSecond, timeSetpoint),

                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}