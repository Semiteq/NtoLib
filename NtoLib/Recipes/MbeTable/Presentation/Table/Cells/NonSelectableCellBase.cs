using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// Base class for DataGridView cells that should not display selection background.
/// Provides consistent painting behavior by drawing cell background explicitly
/// and removing selection paint parts.
/// </summary>
public abstract class NonSelectableCellBase : DataGridViewTextBoxCell
{
    /// <summary>
    /// Paints the cell without selection background, using the provided cell style colors.
    /// </summary>
    protected override void Paint(
        Graphics graphics,
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
        using (var backgroundBrush = new SolidBrush(cellStyle.BackColor))
        {
            graphics.FillRectangle(backgroundBrush, cellBounds);
        }

        var partsWithoutSelection = paintParts
                                    & ~DataGridViewPaintParts.Background
                                    & ~DataGridViewPaintParts.SelectionBackground;

        base.Paint(
            graphics,
            clipBounds,
            cellBounds,
            rowIndex,
            elementState,
            value,
            formattedValue,
            errorText,
            cellStyle,
            advancedBorderStyle,
            partsWithoutSelection);
    }
}