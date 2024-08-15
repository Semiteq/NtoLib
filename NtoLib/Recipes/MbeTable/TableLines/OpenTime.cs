using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class OpenTime : RecipeLine
    {
        public const string Name = Commands.OPEN_TIME;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public OpenTime() : this(1, 1f, "") { }

        public OpenTime(int number, float timeSetpoint, string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._int, number),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._floatSecond, timeSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
        }
    }
}