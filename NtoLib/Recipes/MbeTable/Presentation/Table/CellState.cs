using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table;

public record CellState(
    bool IsReadonly,
    Font Font, 
    Color ForeColor,
    Color BackColor);