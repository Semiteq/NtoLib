using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.CellState;

public record CellStatusDescription(
    bool IsReadonly,
    Font Font, 
    Color ForeColor, // Font color
    Color BackColor // Cell background color
    );