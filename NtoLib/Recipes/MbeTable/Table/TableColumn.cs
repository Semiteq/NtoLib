using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.Table
{
    internal class TableColumn
    {
        public TableColumn(string name, CellType type)
        {
            Name = name;
            Type = type;
        }

        public TableColumn(string name, TableEnumType enumType)
        {
            Name = name;
            Type = CellType._enum;
            EnumType = enumType;
        }

        public string Name { get; }

        public CellType Type { get; }

        public TableEnumType EnumType { get; }

        public int GridIndex { get; set; }
    }
}