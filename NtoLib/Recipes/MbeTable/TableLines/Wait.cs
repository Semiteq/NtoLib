using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.TableLines
{
    internal class Wait : RecipeLine
    {
        public const string Name = Commands.WAIT;
        public override ActionTime ActionTime => ActionTime.TimeSetpoint;

        public Wait() : this(10f, "") { }

        public Wait(float timeSetpoint, string comment) : base(Name)
        {
            int actionNumber = (int)Actions.GetActionNumber(Name);

            _cells = new List<TCell>
            {
                new TCell(CellType._enum, Name, actionNumber),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._floatSecond, timeSetpoint),
                new TCell(CellType._blocked, ""),
                new TCell(CellType._string, comment)
            };
        }
    }
}