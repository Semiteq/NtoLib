using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class OpenTime : RecipeLine
    {
        public const string ActionName = Commands.OPEN_TIME;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public OpenTime(int number = 0, float timeSetpoint = 1f, string comment = "") : base(ActionName)
        {
            shutterName = GrowthList.Instance.ShutterNames[number];
            int actionNumber = Actions[ActionName];
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