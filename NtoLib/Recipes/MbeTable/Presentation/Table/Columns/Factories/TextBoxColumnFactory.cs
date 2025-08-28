using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class TextBoxColumnFactory :  IColumnFactory
{
    public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = colDef.Key.Value,
            HeaderText = colDef.UiName,
            ReadOnly = colDef.ReadOnly,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        
        column.DefaultCellStyle.Alignment = colDef.Alignment;
        column.DefaultCellStyle.Font = colorScheme.LineFont;
        column.DefaultCellStyle.BackColor = colorScheme.LineBgColor;
        
        if (colDef.Width > 0)
        {
            column.Width = colDef.Width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
        else
        {
            column.MinimumWidth = 50;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        
        return column;
    }
}