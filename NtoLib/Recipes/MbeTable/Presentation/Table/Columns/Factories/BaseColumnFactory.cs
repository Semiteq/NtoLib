using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public abstract class BaseColumnFactory : IColumnFactory
{
    /// <inheritdoc >

    private const int MinWidth = 50;

    public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var column = CreateColumnInstance(colDef);

        column.Name = colDef.Key.Value;
        column.HeaderText = colDef.UiName;
        column.SortMode = DataGridViewColumnSortMode.NotSortable;
        column.ReadOnly = colDef.ReadOnly;

        column.DefaultCellStyle.Alignment = colDef.Alignment;
        column.DefaultCellStyle.Font = colorScheme.LineFont;
        column.DefaultCellStyle.BackColor = colorScheme.LineBgColor;
        column.DefaultCellStyle.ForeColor = colorScheme.LineTextColor;

        if (colDef.Width > 0)
        {
            column.Width = colDef.Width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
        else
        {
            column.MinimumWidth = MinWidth;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        ConfigureColumn(column, colDef, colorScheme);

        return column;
    }

    protected abstract DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef);

    protected virtual void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
    }
}