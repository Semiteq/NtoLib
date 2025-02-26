using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureBySpeed : RecipeLine
    {
        public const string ActionName = Commands.TEMP_BY_SPEED;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureBySpeed(int number, float temperatureSetpoint, float temperatureSpeed, string comment) :
            base(ActionName)
        {
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == number).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, number),
                new(CellType._floatTemp, temperatureSetpoint),
                new(CellType._floatTempSpeed, temperatureSpeed),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;

            MinTimeSetpoint = 0.1f;
            MaxTimeSetpoint = 200.0f;
        }
    }
}