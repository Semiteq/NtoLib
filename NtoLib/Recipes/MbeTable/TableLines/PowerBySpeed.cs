using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerBySpeed : RecipeLine
    {
        public const string ActionName = Commands.POWER_BY_SPEED;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerBySpeed(int number = 0, float powerSetpoint = 10f, float speedSetpoint = 1f, string comment = "") : base(ActionName)
        {
            UpdateHeaderToHeat();
            heaterName = GrowthList.HeaterNames[number];
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, heaterName, number),
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