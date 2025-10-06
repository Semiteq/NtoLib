using System.Drawing;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
    private static Color Darken(Color color)
    {
        const int delta = 40;
        int Clamp(int value) => value < 0 ? 0 : value > 255 ? 255 : value;
        return Color.FromArgb(
            color.A, 
            Clamp(color.R - delta), 
            Clamp(color.G - delta), 
            Clamp(color.B - delta));
    }
}

