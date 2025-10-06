using System.Drawing;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Rendering;

/// <summary>
/// Renderer for ComboBox-style cells that draws using final visual attributes.
/// </summary>
public sealed class ComboBoxCellRenderer : ICellRenderer
{
    private const int TextPadding = 3;
    private readonly IColorSchemeProvider _schemeProvider;

    public ComboBoxCellRenderer(IColorSchemeProvider schemeProvider)
    {
        _schemeProvider = schemeProvider;
    }

    public void Render(in CellRenderContext ctx)
    {
        using (var back = new SolidBrush(ctx.BackColor))
        {
            ctx.Graphics.FillRectangle(back, ctx.Bounds);
        }

        var displayText = ctx.FormattedValue?.ToString() ?? string.Empty;
        DrawText(ctx, displayText);
        DrawGridLines(ctx);

        if (ctx.IsCurrent)
            DrawFocus(ctx);
    }

    private static void DrawText(CellRenderContext ctx, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var rect = Rectangle.Inflate(ctx.Bounds, -TextPadding, -2);
        TextRenderer.DrawText(
            ctx.Graphics,
            text,
            ctx.Font,
            rect,
            ctx.ForeColor,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis);
    }

    private static void DrawGridLines(CellRenderContext ctx)
    {
        using var pen = new Pen(SystemColors.ControlDark);
        ctx.Graphics.DrawLine(pen,
            ctx.Bounds.Left,
            ctx.Bounds.Bottom - 1,
            ctx.Bounds.Right,
            ctx.Bounds.Bottom - 1);

        ctx.Graphics.DrawLine(pen,
            ctx.Bounds.Right - 1,
            ctx.Bounds.Top,
            ctx.Bounds.Right - 1,
            ctx.Bounds.Bottom - 1);
    }

    private void DrawFocus(CellRenderContext ctx)
    {
        var scheme = _schemeProvider.Current;
        using var pen = new Pen(scheme.SelectedOutlineColor, scheme.SelectedOutlineThickness);

        var rect = Rectangle.Inflate(ctx.Bounds, -1, -1);
        ctx.Graphics.DrawRectangle(pen, rect);
    }
}