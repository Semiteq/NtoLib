using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class TemperatureByTime : RecipeLine
    {
        public const string Name = Commands.TEMP_BY_TIME;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public TemperatureByTime() : this(1, 500f, 60f, "") { }

        public TemperatureByTime(int number, float temperatureSetpoint, float timeSetpoint, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatTemp, temperatureSetpoint),
                new TCell(CellType._floatSecond, timeSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000f;

            //MinTimeSetpoint = 0.1f;
            //MaxTimeSetpoint = 200.0f;
        }
    }
}
