using NtoLib.Recipes.MbeTable.Utils;

namespace NtoLib.Recipes.MbeTable
{
    public class CellType : Enumeration
    {
        public static readonly CellType _blocked = new CellType(0, "Blocked");

        public static readonly CellType _bool = new CellType(1, "Bool");

        public static readonly CellType _int = new CellType(2, "Int");

        public static readonly CellType _float = new CellType(3, "Float");
        public static readonly CellType _floatPercent = new CellType(4, "FloatPercent");
        public static readonly CellType _floatTemp = new CellType(5, "FloatTemp");
        public static readonly CellType _floatSecond = new CellType(6, "FloatSecond");
        public static readonly CellType _floatTempSpeed = new CellType(7, "FloatDegPerMinute");
        public static readonly CellType _floatPowerSpeed = new CellType(8, "FloatPercentPerMinute");

        public static readonly CellType _enum = new CellType(9, "Enum");

        public static readonly CellType _string = new CellType(10, "String");

        private CellType() { }
        private CellType(int value, string displayName) : base(value, displayName) { }

    }
}