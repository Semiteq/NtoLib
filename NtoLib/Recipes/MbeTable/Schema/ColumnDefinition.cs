using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Recipe.PropertyUnion;

namespace NtoLib.Recipes.MbeTable.Schema
{
    public class ColumnDefinition
    {
        public ColumnKey Key { get; set; } // Unique identifier for the column
        public int Index { get; set; } // Index in the table schema, used for ordering
        public string UiName { get; set; } 
        public PropertyType PropertyType { get; set; }
        public int Width { get; set; } // -1 for auto width
        public bool ReadOnly { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; }
    }
}