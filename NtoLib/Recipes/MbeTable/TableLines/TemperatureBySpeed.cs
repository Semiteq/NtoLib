using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class TemperatureBySpeed : RecipeLine
    {
        public const string Name = Commands.TEMP_BY_SPEED;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureBySpeed() : this(1, 500f, 1f, "") { }

        public TemperatureBySpeed(int number, float temperatureSetpoint, float temperatureSpeed, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatTemp, temperatureSetpoint),
                new TCell(CellType._floatTempSpeed, temperatureSpeed),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;

            MinTimeSetpoint = 0.1f;
            MaxTimeSetpoint = 200.0f;
        }
    }
}