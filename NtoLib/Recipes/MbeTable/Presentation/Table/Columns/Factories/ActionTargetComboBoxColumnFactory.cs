using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class ActionTargetComboBoxColumnFactory : BaseColumnFactory
{
    private const int MaxDropDownItems = 20;

    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new ActionTargetComboBoxColumn();
    }

    /// <inheritdoc />
    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        if (column is not DataGridViewComboBoxColumn comboColumn) return;

        comboColumn.DisplayStyleForCurrentCellOnly = true;
        comboColumn.MaxDropDownItems = MaxDropDownItems;
    }
}