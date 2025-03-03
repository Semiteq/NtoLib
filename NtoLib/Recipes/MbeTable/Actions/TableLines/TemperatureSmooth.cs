using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureSmooth : RecipeLine
    {
        public const string ActionName = Commands.TEMP_SMOOTH;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureSmooth(int actionTarget, float temperatureSetpoint, float initialValue, float speed, float timeSetpoint, string comment) : base(ActionName)
        {
            HeaterName = GrowthList.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, HeaterName, actionTarget),
                new(CellType._floatTemp, temperatureSetpoint),
                new(CellType._floatTemp, initialValue),
                new(CellType._floatTempSpeed, speed),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;

            MinTimeSetpoint = 0.1f;
            MaxTimeSetpoint = 200.0f;
        }
    }
}