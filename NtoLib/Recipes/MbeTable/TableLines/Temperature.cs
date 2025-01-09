using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Temperature : RecipeLine
    {
        public const string Name = Commands.TEMP;
        public override ActionTime ActionTime => ActionTime.Immediately;

        public Temperature() : this(1, 500f, "") { }

        public Temperature(int number, float temperatureSetpoint, string comment) : base(Name)
        {
            int actionNumber = Actions[Name];

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._floatTemp, temperatureSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };

            MinSetpoint = 20f;
            MaxSetpoint = 2000;
        }
    }
}