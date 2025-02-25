using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureByTime : RecipeLine
    {
        public const string ActionName = Commands.TEMP_BY_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureByTime(int number, float temperatureSetpoint, float timeSetpoint, string comment) : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames[number];
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, heaterName, number),
                new(CellType._floatTemp, temperatureSetpoint),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;

            //MinTimeSetpoint = 0.1f;
            //MaxTimeSetpoint = 200.0f;
        }
    }
}
