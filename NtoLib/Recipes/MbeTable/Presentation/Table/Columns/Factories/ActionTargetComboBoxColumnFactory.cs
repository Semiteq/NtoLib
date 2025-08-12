#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories
{
    public class ActionTargetComboBoxColumnFactory : IColumnFactory
    {
        public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
        {
            var comboColumn = new ActionTargetComboBoxColumn
            {
                Name = colDef.Key.ToString(),
                HeaderText = colDef.UiName,
                DataPropertyName = nameof(StepViewModel.ActionTarget),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DisplayStyleForCurrentCellOnly = true
            };

            comboColumn.DefaultCellStyle.Alignment = colDef.Alignment;
            comboColumn.DefaultCellStyle.Font = colorScheme.LineFont;
            comboColumn.DefaultCellStyle.BackColor = colorScheme.LineBgColor;

            comboColumn.MaxDropDownItems = 20;
            comboColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

            if (colDef.Width > 0)
            {
                comboColumn.Width = colDef.Width;
                comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
            else
            {
                comboColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            return comboColumn;
        }
    }
}