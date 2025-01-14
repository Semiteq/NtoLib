using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerByTime : RecipeLine
    {
        public const string ActionName = Commands.POWER_BY_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerByTime(int number = 0, float powerSetpoint = 10f, float timeSetpoint = 60f, string comment = "") : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames.GetValueByIndex(number);
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, heaterName, number),
                new(CellType._floatPercent, powerSetpoint),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;

            //MaxTimeSetpoint = 120.0f;
        }
    }
}
