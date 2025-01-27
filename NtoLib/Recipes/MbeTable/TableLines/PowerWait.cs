using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerWait : RecipeLine
    {
        public const string ActionName = Commands.POWER_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public PowerWait(int number = 0, float powerSetpoint = 10f, float timeSetpoint = 60f, string comment = "") : base(ActionName)
        {
            heaterName = GrowthList.Instance.HeaterNames[number];
            int actionNumber = Actions.Names[ActionName];
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

        }
    }
}