using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureWait : RecipeLine
    {
        public const string ActionName = Commands.TEMP_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public TemperatureWait(int number, float temperatureSetpoint, float timeSetpoint, string comment) : base(ActionName)
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
            MaxSetpoint = 2000;
        }
    }
}