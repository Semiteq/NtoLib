using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class TemperatureSmooth : RecipeLine
    {
        public const string ActionName = Commands.TEMP_SMOOTH;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureSmooth(int actionTarget, float initialValue, float temperatureSetpoint, float speed, float timeSetpoint, string comment) : base(ActionName)
        {
            HeaterName = ActionTarget.HeaterNames.FirstOrDefault(x => x.Key == actionTarget).Value;
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            Cells = new List<TCell>
            {
                new(CellType.Enum, ActionName, actionNumber),
                new(CellType.Enum, HeaterName, actionTarget),

                new(CellType.FloatTemp, initialValue),
                new(CellType.FloatTemp, temperatureSetpoint),

                new(CellType.FloatTempSpeed, speed),
                new(CellType.FloatSecond, timeSetpoint),

                new(CellType.Blocked, ""),
                new(CellType.String, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;
        }
    }
}