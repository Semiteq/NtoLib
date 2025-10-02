using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Base factory for creating DataGridView columns with consistent styling.
/// Subclasses override CreateColumnInstance to provide specific column types.
/// </summary>
public abstract class BaseColumnFactory
{
    private const int DefaultMinWidth = 50;
    private const int CriticalMinWidth = 2; // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridviewcolumn.minimumwidth?view=windowsdesktop-9.0#property-value
    protected IComboBoxContext ComboBoxContext { get; }

    protected BaseColumnFactory(IComboBoxContext comboBoxContext)
    {
        ComboBoxContext = comboBoxContext;
    }

    /// <summary>
    /// Creates and configures a DataGridView column based on column definition.
    /// </summary>
    /// <param name="columnDefinition">Column configuration from YAML.</param>
    /// <param name="colorScheme">Color scheme for default cell styling.</param>
    /// <returns>Configured DataGridView column ready for addition to DataGridView.</returns>
    public DataGridViewColumn CreateColumn(ColumnDefinition columnDefinition, ColorScheme colorScheme)
    {
        var column = CreateColumnInstance(columnDefinition);

        column.Name = columnDefinition.Key.Value;
        column.HeaderText = columnDefinition.UiName;
        column.SortMode = DataGridViewColumnSortMode.NotSortable;
        column.ReadOnly = columnDefinition.ReadOnly;
        
        column.DefaultCellStyle.Alignment = columnDefinition.Alignment;
        column.DefaultCellStyle.Font = colorScheme.LineFont;
        column.DefaultCellStyle.BackColor = colorScheme.LineBgColor;
        column.DefaultCellStyle.ForeColor = colorScheme.LineTextColor;
        
        column.MinimumWidth = columnDefinition.MinimalWidth;
        column.Width = columnDefinition.Width;
        
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        
        if (columnDefinition.MinimalWidth < CriticalMinWidth) column.MinimumWidth = DefaultMinWidth;
        if (columnDefinition.Width < CriticalMinWidth) column.Width = columnDefinition.MinimalWidth;
        if (columnDefinition.Width == -1) column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        
        ConfigureColumn(column, columnDefinition, colorScheme);
        return column;
    }

    /// <summary>
    /// Creates the specific column type. Implemented by subclasses.
    /// </summary>
    protected abstract DataGridViewColumn CreateColumnInstance(ColumnDefinition columnDefinition);

    /// <summary>
    /// Performs additional column-specific configuration. Override if needed.
    /// </summary>
    protected virtual void ConfigureColumn(DataGridViewColumn column, ColumnDefinition columnDefinition, ColorScheme colorScheme)
    {
    }
}