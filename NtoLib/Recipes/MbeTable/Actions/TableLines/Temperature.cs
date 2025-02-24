using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Actions.TableLines
{
    internal class Temperature : RecipeLine
    {
        public const string ActionName = Commands.TEMP;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Temperature(int number, float temperatureSetpoint, string comment) : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames[number];
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
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