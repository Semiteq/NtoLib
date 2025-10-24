using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

public abstract class FactoryColumnComboBoxBase : FactoryColumnBase
{
    protected readonly IServiceProvider ServiceProvider;

    protected FactoryColumnComboBoxBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
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
            ValueType = typeof(short?),
            MaxDropDownItems = definition.MaxDropdownItems,
            DataPropertyName = definition.Key.Value
        };

        return column;
    }

    protected override void ConfigureColumn(DataGridViewColumn column)
    {
        if (column is not DataGridViewComboBoxColumn combo) return;

        combo.DataSource = GetDataSource();
        combo.DisplayMember = "Value";
        combo.ValueMember = "Key";
    }

    protected abstract IList<KeyValuePair<short, string>> GetDataSource();

    protected abstract void AssignItemsProvider(RecipeComboBoxCell cell);

    private RecipeComboBoxCell CreateCell()
    {
        var cell = new RecipeComboBoxCell();
        AssignItemsProvider(cell);
        cell.SetRenderer(ServiceProvider.GetRequiredService<ICellRenderer>());
        return cell;
    }
}