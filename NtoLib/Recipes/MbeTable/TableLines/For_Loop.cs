using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class For_Loop : RecipeLine
    {
        public const string ActionName = Commands.FOR;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public For_Loop(int setpoint = 5, string comment = "") : base(ActionName)
        {
            int actionNumber = Actions.Names[ActionName];

            _cells = new List<TCell>
            {
                new(CellType._enum, ActionName, actionNumber),
                new(CellType._blocked, ""),
                new(CellType._float, (float)setpoint),
                new(CellType._blocked, ""),
                new(CellType._blocked, ""),
                new(CellType._string, comment)
            };

            MinSetpoint = 1;
            MaxSetpoint = 100f;
        }
    }
}