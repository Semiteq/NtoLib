using System.Drawing;

namespace NtoLib.Recipes.MbeTable.Presentation.Table;

public class CellState
{
    public  Font Font { get; set; }
    public  Color ForeColor { get; set; }
    public  Color BackColor { get; set; }

    public CellState(Font font, Color foreColor, Color backColor)
    {
        Font = font;
        ForeColor = foreColor;
        BackColor = backColor;
    }
}