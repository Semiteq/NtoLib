using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Power : RecipeLine
    {
        public const string ActionName = Commands.POWER;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Power(int number = 1, float powerSetpoint = 10f, string comment = "") : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames.GetValueByIndex(number);
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, heaterName, number),
                new(CellType._floatPercent, powerSetpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}