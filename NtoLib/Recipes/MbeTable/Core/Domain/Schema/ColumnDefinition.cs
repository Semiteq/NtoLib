using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Schema
{
    public class ColumnDefinition
    {
        public ColumnKey Key { get; set; } // Unique identifier for the column
        public int Index { get; set; } // Index in the table schema, used for ordering
        public string UiName { get; set; } 
        public PropertyType PropertyType { get; set; }
        public Type Type { get; set; } // Type of the property, e.g., int, float, string
        public Type TableCellType { get; set; } // Type of the cell in the DataGridView, e.g., DataGridViewTextBoxCell, DataGridViewComboBoxCell
        public int Width { get; set; } // -1 for auto width
        public bool ReadOnly { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; }
    }
}