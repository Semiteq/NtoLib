#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class ActionComboBoxColumnFactory : BaseColumnFactory
{
    private readonly IComboboxDataProvider _dataProvider;
    private const int MaxDropDownItems = 20;

    public ActionComboBoxColumnFactory(IComboboxDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new DataGridViewComboBoxColumn();
    }

    /// <inheritdoc />
    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        if (column is not DataGridViewComboBoxColumn comboColumn) return;

        comboColumn.DataSource = _dataProvider.GetActions();
        comboColumn.DisplayMember = "Value";
        comboColumn.ValueMember = "Key";
        comboColumn.ValueType = typeof(int?);
        comboColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        comboColumn.DisplayStyleForCurrentCellOnly = true;
        comboColumn.FlatStyle = FlatStyle.Standard;
        comboColumn.MaxDropDownItems = MaxDropDownItems;
    }
}