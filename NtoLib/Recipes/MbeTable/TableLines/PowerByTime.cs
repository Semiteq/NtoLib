using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerByTime : RecipeLine
    {
        public const string Name = Commands.POWER_BY_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerByTime() : this(1, 10f, 60f, "") { }

        public PowerByTime(int number, float powerSetpoint, float timeSetpoint, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatPercent, powerSetpoint),
                new TCell(CellType._floatSecond, timeSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;

            //MaxTimeSetpoint = 120.0f;
        }
    }
}
