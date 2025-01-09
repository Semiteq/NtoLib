using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class For_Loop : RecipeLine
    {
        public const string Name = Commands.FOR;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public For_Loop() : this(5, "") { }

        public For_Loop(int setpoint, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._float, (float)setpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 1;
            MaxSetpoint = 100f;
        }
    }
}