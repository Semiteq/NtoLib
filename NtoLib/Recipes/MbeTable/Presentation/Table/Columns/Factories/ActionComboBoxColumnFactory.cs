#nullable enable
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class ActionComboBoxColumnFactory : BaseColumnFactory
{
    private readonly IComboboxDataProvider _dataProvider;

    public ActionComboBoxColumnFactory(IComboboxDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new ActionComboBoxColumn();
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        if (column is not DataGridViewComboBoxColumn comboColumn) return;
        comboColumn.DataSource = _dataProvider.GetActions();
        comboColumn.DisplayMember = "Value";
        comboColumn.ValueMember = "Key";
    }
}