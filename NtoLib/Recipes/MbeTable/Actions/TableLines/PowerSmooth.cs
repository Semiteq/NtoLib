using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class PowerSmooth : RecipeLine
    {
        public const string ActionName = Commands.POWER_SMOOTH;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerSmooth(int actionTarget, float powerSetpoint, float initialValue, float speed, float timeSetpoint, string comment) : base(ActionName)
        {
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, actionTarget),
                new(CellType._floatPercent, powerSetpoint),
                new(CellType._floatPercent, initialValue),
                new(CellType._floatPowerSpeed, speed),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;

            MinTimeSetpoint = 0.1f;
            MaxTimeSetpoint = 200.0f;
        }
    }
}