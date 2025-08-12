using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.State;

public record CellState(
    bool IsReadonly,
    Font Font, 
    Color ForeColor,
    Color BackColor);