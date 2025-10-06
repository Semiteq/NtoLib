using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Presentation.Cells;
using NtoLib.Recipes.MbeTable.Presentation.DataAccess;
using NtoLib.Recipes.MbeTable.Presentation.Rendering;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

public sealed class ActionTargetComboBoxColumnFactory : BaseColumnFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ActionTargetComboBoxColumnFactory(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition)
    {
        var cellTemplate = CreateCell();

        var column = new DataGridViewComboBoxColumn
        {
            CellTemplate = cellTemplate,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            DisplayStyleForCurrentCellOnly = true,
            ValueType = typeof(int?),
            MaxDropDownItems = definition.MaxDropdownItems,
            DataPropertyName = definition.Key.Value
        };

        return column;
    }

    protected override void ConfigureColumn(
        DataGridViewColumn column,
        ColumnDefinition definition,
        ColorScheme scheme)
    {
        if (column is DataGridViewComboBoxColumn combo)
        {
            combo.DataSource = new List<KeyValuePair<int, string>>();
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";
        }
    }

    private RecipeComboBoxCell CreateCell()
    {
        var cell = new RecipeComboBoxCell();

        cell.SetItemsProvider(_serviceProvider.GetRequiredService<TargetItemsProvider>());
        cell.SetRenderer(_serviceProvider.GetRequiredService<ICellRenderer>());

        return cell;
    }
}