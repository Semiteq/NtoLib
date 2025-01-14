using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Temperature : RecipeLine
    {
        public const string ActionName = Commands.TEMP;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Temperature(int number = 0, float temperatureSetpoint = 500f, string comment = "") : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames.GetValueByIndex(number);
            int actionNumber = Actions[ActionName];
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, heaterName, number),
                new(CellType._floatTemp, temperatureSetpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000;
        }
    }
}