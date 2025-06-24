using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Table.UI.TableUpdate;

public class CellState
{
    private Font Font { get; set; }
    private Color ForeColor { get; set; }
    private Color BackColor { get; set; }
    public bool Blocked { get; set; }

    public CellState(Font font, Color foreColor, Color backColor, bool blocked = false)
    {
        Font = font;
        ForeColor = foreColor;
        BackColor = backColor;
        Blocked = blocked;
    }
    
    public void ApplyTo(DataGridViewCell cell)
    {
        if (cell == null) return;

        cell.Style.Font = Font;
        cell.Style.ForeColor = ForeColor;
        cell.Style.BackColor = BackColor;
        cell.ReadOnly = Blocked;
    }
}