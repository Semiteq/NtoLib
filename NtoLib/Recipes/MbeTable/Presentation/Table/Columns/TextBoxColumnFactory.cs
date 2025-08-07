using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

public class TextBoxColumnFactory :  IColumnFactory
{
    public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = colDef.Key.ToString(),
            Name = colDef.Key.ToString(),
            HeaderText = colDef.UiName,
            ReadOnly = colDef.ReadOnly,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        
        column.DefaultCellStyle.Alignment = colDef.Alignment;
        column.DefaultCellStyle.Font = colorScheme.LineFont;
        column.DefaultCellStyle.BackColor = colorScheme.LineBgColor;
        column.DefaultCellStyle.Font = colorScheme.LineFont;
        
        if (colDef.Width > 0)
        {
            column.Width = colDef.Width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
        else
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        
        return column;
    }
}