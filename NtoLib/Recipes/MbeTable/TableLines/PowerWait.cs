using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerWait : RecipeLine
    {
        public const string Name = Commands.POWER_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public PowerWait() : this(1, 10f, 60f, "") { }

        public PowerWait(int number, float powerSetpoint, float timeSetpoint, string comment) : base(Name)
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

        }
    }
}