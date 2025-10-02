#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.DataSource;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates DataGridView ComboBox columns for the Action selection column.
/// Uses static column-level datasource strategy.
/// </summary>
public sealed class ActionComboBoxColumnFactory : BaseColumnFactory
{
    private readonly IComboboxDataProvider _comboboxDataProvider;

    public ActionComboBoxColumnFactory(IComboBoxContext comboBoxContext, IComboboxDataProvider comboboxDataProvider) 
        : base(comboBoxContext)
    {
        _comboboxDataProvider = comboboxDataProvider;
    }

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition columnDefinition)
    {
        var column = new DataGridViewComboBoxColumn
        {
            CellTemplate = new RecipeComboBoxCell(),
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            DisplayStyleForCurrentCellOnly = true,
            ValueType = typeof(int?),
            MaxDropDownItems = columnDefinition.MaxDropdownItems
        };

        column.Tag = typeof(ColumnStaticDataSource);

        return column;
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition columnDefinition, ColorScheme colorScheme)
    {
        if (column is not DataGridViewComboBoxColumn comboColumn)
        {
            return;
        }

        comboColumn.DataSource = _comboboxDataProvider.GetActions();
        comboColumn.DisplayMember = "Value";
        comboColumn.ValueMember = "Key";
    }
}