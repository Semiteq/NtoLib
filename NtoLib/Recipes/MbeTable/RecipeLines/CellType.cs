using NtoLib.Recipes.MbeTable.Utils;

namespace NtoLib.Recipes.MbeTable.RecipeLines
{
    public class CellType : Enumeration
    {
        public static readonly CellType _blocked = new(0, "Blocked");

        public static readonly CellType _bool = new(1, "Bool");

        public static readonly CellType _int = new(2, "Int");

        public static readonly CellType _float = new(3, "Float");
        public static readonly CellType _floatPercent = new(4, "FloatPercent");
        public static readonly CellType _floatTemp = new(5, "FloatTemp");
        public static readonly CellType _floatSecond = new(6, "FloatSecond");
        public static readonly CellType _floatTempSpeed = new(7, "FloatDegPerMinute");
        public static readonly CellType _floatPowerSpeed = new(8, "FloatPercentPerMinute");

        public static readonly CellType _enum = new(9, "Enum");

        public static readonly CellType _string = new(10, "String");

        private CellType(int value, string displayName) : base(value, displayName) { }
    }
}