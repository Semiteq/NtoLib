using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureWait : RecipeLine
    {
        public const string ActionName = Commands.TEMP_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public TemperatureWait(int number, float temperatureSetpoint, float timeSetpoint, string comment) : base(
            ActionName)
        {
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == number).Value;
            var actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, number),
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