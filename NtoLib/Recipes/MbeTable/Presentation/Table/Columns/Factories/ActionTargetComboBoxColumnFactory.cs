#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.DataSource;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates DataGridView ComboBox columns for ActionTarget selection (e.g., valves, sensors).
/// Uses dynamic row-level datasource strategy.
/// </summary>
public sealed class ActionTargetComboBoxColumnFactory : BaseColumnFactory
{
    public ActionTargetComboBoxColumnFactory(IComboBoxContext comboBoxContext) 
        : base(comboBoxContext)
    {
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

        column.Tag = typeof(RowDynamicDataSource);

        return column;
    }
}