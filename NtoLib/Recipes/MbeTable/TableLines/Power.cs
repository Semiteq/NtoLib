using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Power : RecipeLine
    {
        public const string Name = Commands.POWER;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Power() : this(1, 10f, "") { }

        public Power(int number, float powerSetpoint, string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatPercent, powerSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;
        }
    }
}