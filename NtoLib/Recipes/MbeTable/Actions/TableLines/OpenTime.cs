using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Actions;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class OpenTime : RecipeLine
    {
        public const string ActionName = Commands.OPEN_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public OpenTime(int number, float timeSetpoint, string comment) : base(ActionName)
        {
            shutterName = GrowthList.ShutterNames[number];
            int actionNumber = ActionManager.GetActionIdByCommand(ActionName);
            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._enum, shutterName, number),
                new(CellType._blocked, ""),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}