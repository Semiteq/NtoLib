#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Presentation.Table.Cells;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories
{
    public class StepStartTimeColumnFactory : IColumnFactory
    {
        public DataGridViewColumn CreateColumn(ColumnDefinition colDef, ColorScheme colorScheme)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = colDef.Key.ToString(),
                HeaderText = colDef.UiName,
                //DataPropertyName = nameof(StepViewModel.StepStartTime),
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            column.CellTemplate = new ReadonlyLabelCell();
            column.DefaultCellStyle.Alignment = colDef.Alignment;
            column.DefaultCellStyle.Font = colorScheme.LineFont;
            column.DefaultCellStyle.BackColor = colorScheme.BlockedBgColor.IsEmpty ? colorScheme.LineBgColor : colorScheme.BlockedBgColor;
            column.DefaultCellStyle.ForeColor = colorScheme.BlockedTextColor.IsEmpty ? colorScheme.LineTextColor : colorScheme.BlockedTextColor;

            if (colDef.Width > 0)
            {
                column.Width = colDef.Width;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
            else
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            return column;
        }
    }
}