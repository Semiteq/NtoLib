using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.CellState;
    public readonly record struct CellStatusDescription(
        bool IsReadonly,
        Font Font,
        Color ForeColor,
        Color BackColor);
