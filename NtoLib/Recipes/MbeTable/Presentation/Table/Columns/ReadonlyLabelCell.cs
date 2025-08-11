#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns
{
    public class ReadonlyLabelCell : DataGridViewTextBoxCell
    {
        protected override void Paint(Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates elementState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            using (var back = new SolidBrush(cellStyle.BackColor))
            {
                graphics.FillRectangle(back, cellBounds);
            }

            var parts = paintParts &
                        ~DataGridViewPaintParts.Background &
                        ~DataGridViewPaintParts.SelectionBackground;

            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState,
                value, formattedValue, errorText, cellStyle, advancedBorderStyle, parts);
        }
    }
}