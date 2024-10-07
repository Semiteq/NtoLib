using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class PowerBySpeed : RecipeLine
    {
        public const string Name = Commands.POWER_BY_SPEED;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public PowerBySpeed() : this(1, 10f, 1f, "") { }

        public PowerBySpeed(int number, float powerSetpoint, float speedSetpoint, string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatPercent, powerSetpoint),
                new TCell(CellType._floatPowerSpeed, speedSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 0f;
            MaxSetpoint = 100f;

            MaxTimeSetpoint = 120.0f;
        }
    }
}