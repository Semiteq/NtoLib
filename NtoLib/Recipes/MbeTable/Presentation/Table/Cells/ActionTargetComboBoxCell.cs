#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Table.Editing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// Per-row ComboBox cell for ActionTarget. Handles per-row data binding and formatting.
/// </summary>
public class ActionTargetComboBoxCell : DataGridViewComboBoxCell
{
    public ActionTargetComboBoxCell()
    {
        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        FlatStyle = FlatStyle.Standard;
        ValueType = typeof(int?);
        DisplayMember = "Value";
        ValueMember = "Key";
    }

    public override Type EditType => typeof(ActionTargetEditingControl);

    public override object Clone()
    {
        var copy = (ActionTargetComboBoxCell)base.Clone();
        return copy;
    }

    public override void InitializeEditingControl(int rowIndex, object formattedValue, DataGridViewCellStyle cellStyle)
    {
        base.InitializeEditingControl(rowIndex, formattedValue, cellStyle);

        if (DataGridView?.EditingControl is not ActionTargetEditingControl ctl)
            return;

        var vm = GetRowViewModel(rowIndex);
        var list = vm?.AvailableActionTargets;

        this.DataSource = list;
        this.DisplayMember = "Value";
        this.ValueMember = "Key";

        ctl.DataSource = list;
        ctl.DisplayMember = "Value";
        ctl.ValueMember = "Key";
        ctl.DropDownStyle = ComboBoxStyle.DropDownList;

        var rawValue = this.Value;
        if (rawValue is int i)
        {
            ctl.SelectedValue = i;
        }
        else if (rawValue is IConvertible conv)
        {
            try
            {
                ctl.SelectedValue = conv.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                /* ignore */
            }
        }
        else
        {
            ctl.SelectedIndex = -1;
        }

        ctl.ApplyStyleFromCurrentCell();
    }

    protected override object GetFormattedValue(object value,
        int rowIndex,
        ref DataGridViewCellStyle cellStyle,
        TypeConverter valueTypeConverter,
        TypeConverter formattedValueTypeConverter,
        DataGridViewDataErrorContexts context)
    {
        var vm = GetRowViewModel(rowIndex);
        if (vm == null)
            return base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter,
                formattedValueTypeConverter, context);

        if (vm.IsPropertyDisabled(WellKnownColumns.ActionTarget))
            return string.Empty;

        int? key = null;
        if (value is int i) key = i;
        else if (value is IConvertible conv)
        {
            try
            {
                key = conv.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                /* ignore */
            }
        }

        if (key.HasValue)
        {
            var display = vm.AvailableActionTargets.FirstOrDefault(p => p.Key == key.Value).Value;
            if (!string.IsNullOrEmpty(display))
                return display;
        }

        return string.Empty;
    }

    private StepViewModel? GetRowViewModel(int rowIndex)
    {
        if (rowIndex < 0 || DataGridView == null) return null;
        var row = DataGridView.Rows[rowIndex];
        return row?.DataBoundItem as StepViewModel;
    }
}