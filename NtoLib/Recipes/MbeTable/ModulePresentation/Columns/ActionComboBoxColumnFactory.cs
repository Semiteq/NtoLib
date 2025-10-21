using System;
using System.Linq;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public sealed class ActionComboBoxColumnFactory : BaseColumnFactory
{
    private readonly IComboboxDataProvider _comboProvider;
    private readonly IServiceProvider _serviceProvider;

    public ActionComboBoxColumnFactory(
        IComboboxDataProvider comboProvider,
        IServiceProvider serviceProvider)
    {
        _comboProvider = comboProvider;
        _serviceProvider = serviceProvider;
    }

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
        if (column is not DataGridViewComboBoxColumn combo) return;

        combo.DataSource = _comboProvider
            .GetActions()
            .Select(kv => kv)
            .ToList();
        combo.DisplayMember = "Value";
        combo.ValueMember = "Key";
    }

    private RecipeComboBoxCell CreateCell()
    {
        var cell = new RecipeComboBoxCell();

        cell.SetItemsProvider(_serviceProvider.GetRequiredService<ActionItemsProvider>());
        cell.SetRenderer(_serviceProvider.GetRequiredService<ICellRenderer>());

        return cell;
    }
}