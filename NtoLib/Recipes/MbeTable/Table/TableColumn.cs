using System.Collections.Generic;
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

        public TableColumn(string name, Dictionary<int, string> intStringMap)
        {
            Name = name;
            Type = CellType.Enum;
            IntStringMap = intStringMap;
        }

        public string Name { get; }

        public CellType Type { get; }

        public Dictionary<int, string> IntStringMap { get; }

        public int GridIndex { get; set; }
    }
}