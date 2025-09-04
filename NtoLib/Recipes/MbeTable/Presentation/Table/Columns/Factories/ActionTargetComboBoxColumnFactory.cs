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
        
        comboColumn.DisplayMember = "Value";
        comboColumn.ValueMember = "Key";
        comboColumn.ValueType = typeof(int?);
        comboColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        comboColumn.DisplayStyleForCurrentCellOnly = true;
        comboColumn.FlatStyle = FlatStyle.Standard;
        comboColumn.MaxDropDownItems = MaxDropDownItems;
    }
}