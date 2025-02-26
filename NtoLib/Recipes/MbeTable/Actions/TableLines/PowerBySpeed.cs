using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class PowerBySpeed : RecipeLine
    {
        public const string ActionName = Commands.POWER_BY_SPEED;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerBySpeed(int number, float powerSetpoint, float speedSetpoint, string comment) : base(ActionName)
        {
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == number).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, number),
                new(CellType._floatPercent, powerSetpoint),
                new(CellType._floatPowerSpeed, speedSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;

            MaxTimeSetpoint = 120.0f;
        }
    }
}