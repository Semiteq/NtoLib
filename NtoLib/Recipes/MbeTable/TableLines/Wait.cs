using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Wait : RecipeLine
    {
        public const string ActionName = Commands.WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public Wait(float timeSetpoint = 10f, string comment = "") : base(ActionName)
        {
            int actionNumber = Actions[ActionName];

            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._floatSecond, timeSetpoint),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };
        }
    }
}