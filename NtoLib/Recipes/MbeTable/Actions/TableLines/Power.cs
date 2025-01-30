using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Power : RecipeLine
    {
        public const string ActionName = Commands.POWER;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Power(int number, float powerSetpoint, string comment) : base(ActionName)
        {
            heaterName = GrowthList.HeaterNames[number];
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
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