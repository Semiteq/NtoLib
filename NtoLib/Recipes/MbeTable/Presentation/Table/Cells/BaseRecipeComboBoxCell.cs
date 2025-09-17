#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Table.Editing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;


public abstract class BaseRecipeComboBoxCell : DataGridViewComboBoxCell
{
    protected BaseRecipeComboBoxCell()
    {
        FlatStyle = FlatStyle.Flat;
        ValueType = typeof(int?);
        DisplayMember = "Value";
        ValueMember = "Key";
        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        DisplayStyleForCurrentCellOnly = true;
        MaxDropDownItems = 30;
    }

    public override Type EditType => typeof(BaseRecipeComboBoxEditingControl);
    
    protected virtual List<KeyValuePair<int, string>>? ProvideRowItems(StepViewModel vm, ColumnIdentifier key) => null;

    public override object Clone()
    {
        var copy = (BaseRecipeComboBoxCell)base.Clone();
        copy.FlatStyle = FlatStyle.Flat;
        return copy;
    }

    public override void InitializeEditingControl(int rowIndex, object? formattedValue, DataGridViewCellStyle cellStyle)
    {
        base.InitializeEditingControl(rowIndex, formattedValue, cellStyle);

        if (DataGridView?.EditingControl is not BaseRecipeComboBoxEditingControl ctl)
            return;

        var vm = GetRowViewModel(rowIndex);
        if (vm == null) return;

        var columnKey = GetCurrentColumnKey();
        var list = ProvideRowItems(vm, columnKey);
        
        if (list != null)
        {
            DataSource = list;
            ctl.DataSource = list;
        }
        else
        {
            ctl.DataSource = ((DataGridViewComboBoxColumn)OwningColumn).DataSource;
        }

        ctl.DisplayMember = DisplayMember;
        ctl.ValueMember = ValueMember;
        ctl.DropDownStyle = ComboBoxStyle.DropDownList;
        ctl.FlatStyle = FlatStyle.Flat;

        var rawValue = Value;
        int? key = CoerceToNullableInt(rawValue);

        if (key.HasValue)
            ctl.SelectedValue = key.Value;
        else
            ctl.SelectedIndex = -1;

        ctl.ApplyStyleFromCurrentCell();
    }

    protected override object? GetFormattedValue(object? value, int rowIndex, ref DataGridViewCellStyle cellStyle,
        TypeConverter? valueTypeConverter, TypeConverter? formattedValueTypeConverter, DataGridViewDataErrorContexts context)
    {
        var vm = GetRowViewModel(rowIndex);
        if (vm == null) return string.Empty;
        var columnKey = GetCurrentColumnKey();

        if (vm.IsPropertyDisabled(columnKey))
            return string.Empty;

        var key = CoerceToNullableInt(value);
        if (!key.HasValue) return string.Empty;

        var dynamicList = ProvideRowItems(vm, columnKey);
        if (dynamicList != null)
        {
            var display = dynamicList.FirstOrDefault(p => p.Key == key.Value).Value;
            return display ?? string.Empty;
        }

        if (OwningColumn is DataGridViewComboBoxColumn cbc && cbc.DataSource is IEnumerable<KeyValuePair<int, string>> colList)
        {
            var display = colList.FirstOrDefault(p => p.Key == key.Value).Value;
            return display ?? string.Empty;
        }

        return base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context) ?? string.Empty;
    }

    private int? CoerceToNullableInt(object? raw)
    {
        switch (raw)
        {
            case null: return null;
            case int i: return i;
            case IConvertible conv:
                try { return conv.ToInt32(CultureInfo.InvariantCulture); }
                catch { return null; }
            default:
                return null;
        }
    }

    protected StepViewModel? GetRowViewModel(int rowIndex)
    {
        if (rowIndex < 0 || DataGridView == null) return null;
        return DataGridView.Rows[rowIndex].DataBoundItem as StepViewModel;
    }

    protected ColumnIdentifier GetCurrentColumnKey()
    {
        if (DataGridView == null || ColumnIndex < 0)
            return new ColumnIdentifier(string.Empty);
        var name = DataGridView.Columns[ColumnIndex].Name;
        return new ColumnIdentifier(name);
    }
}