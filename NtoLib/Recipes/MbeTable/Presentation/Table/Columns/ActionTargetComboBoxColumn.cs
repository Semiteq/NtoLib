#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns
{
    public class ActionTargetComboBoxColumn : DataGridViewComboBoxColumn
    {
        public ActionTargetComboBoxColumn()
        {
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            FlatStyle = FlatStyle.Standard;
            ValueType = typeof(int?);
            CellTemplate = new ActionTargetComboBoxCell();
        }

        public override object Clone()
        {
            var copy = (ActionTargetComboBoxColumn)base.Clone();
            return copy;
        }

        public override DataGridViewCell CellTemplate
        {
            get => base.CellTemplate!;
            set
            {
                if (value != null && !typeof(ActionTargetComboBoxCell).IsAssignableFrom(value.GetType()))
                    throw new InvalidOperationException("CellTemplate must be an ActionTargetComboBoxCell");
                base.CellTemplate = value;
            }
        }
    }

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
                try { ctl.SelectedValue = conv.ToInt32(System.Globalization.CultureInfo.InvariantCulture); }
                catch { /* ignore */ }
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
                return base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);

            if (vm.IsPropertyDisabled(ColumnKey.ActionTarget))
                return string.Empty;

            int? key = null;
            if (value is int i) key = i;
            else if (value is IConvertible conv)
            {
                try { key = conv.ToInt32(System.Globalization.CultureInfo.InvariantCulture); }
                catch { /* ignore */ }
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

    internal sealed class ActionTargetEditingControl : DataGridViewComboBoxEditingControl
    {
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ForceNormalDrawMode();
            ApplyStyleFromCurrentCell();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            ForceNormalDrawMode();
            ApplyStyleFromCurrentCell();
        }

        private void ForceNormalDrawMode()
        {
            DrawMode = DrawMode.Normal;
            FlatStyle = FlatStyle.Standard;
            DropDownStyle = ComboBoxStyle.DropDownList;
            IntegralHeight = false;
        }

        public void ApplyStyleFromCurrentCell()
        {
            var dgv = EditingControlDataGridView;
            if (dgv == null) return;

            var cell = dgv.CurrentCell;
            var style = cell?.InheritedStyle;
            if (style == null) return;

            try
            {
                BackColor = style.BackColor;
                ForeColor = style.ForeColor;
                Font = style.Font;
            }
            catch
            {
                // ignore styling errors
            }
        }
    }
}