#nullable enable

using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

    /// <summary>
    /// DataGridViewComboBoxColumn for ActionTarget with a custom cell template.
    /// </summary>
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
