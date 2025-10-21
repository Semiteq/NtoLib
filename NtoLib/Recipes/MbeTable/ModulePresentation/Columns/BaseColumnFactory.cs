using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public abstract class BaseColumnFactory : IColumnFactory
{
    private const int DefaultMinWidth   = 50;
    private const int CriticalMinWidth  = 2;

    public DataGridViewColumn CreateColumn(ColumnDefinition definition, ColorScheme scheme)
    {
        var column = CreateColumnInstance(definition);

        column.Name                             = definition.Key.Value;
        column.DataPropertyName                 = definition.Key.Value;
        column.HeaderText                       = definition.UiName;
        column.SortMode                         = DataGridViewColumnSortMode.NotSortable;
        column.ReadOnly                         = definition.ReadOnly;
        column.DefaultCellStyle.Alignment       = definition.Alignment;
        column.DefaultCellStyle.Font            = scheme.LineFont;
        column.DefaultCellStyle.BackColor       = scheme.LineBgColor;
        column.DefaultCellStyle.ForeColor       = scheme.LineTextColor;
        column.MinimumWidth                     = definition.MinimalWidth;
        column.Width                            = definition.Width;
        column.AutoSizeMode                     = DataGridViewAutoSizeColumnMode.None;

        if (definition.MinimalWidth < CriticalMinWidth) column.MinimumWidth = DefaultMinWidth;
        if (definition.Width         < CriticalMinWidth) column.Width       = definition.MinimalWidth;
        if (definition.Width == -1)                     column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        ConfigureColumn(column, definition, scheme);
        return column;
    }

    protected abstract DataGridViewColumn CreateColumnInstance(ColumnDefinition definition);

    protected virtual void ConfigureColumn(
        DataGridViewColumn column,
        ColumnDefinition   definition,
        ColorScheme        scheme)
    { }
}