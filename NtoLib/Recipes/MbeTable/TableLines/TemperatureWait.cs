using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class TemperatureWait : RecipeLine
    {
        public const string Name = Commands.TEMP_WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public TemperatureWait() : this(1, 500f, 60f, "") { }

        public TemperatureWait(int number, float temperatureSetpoint, float timeSetpoint, string comment) : base(Name)
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
            MaxSetpoint = 2000;
        }
    }
}