#nullable enable

using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

/// <summary>
/// Maps logical cell state (TableCellState) to a visual style (CellStatusDescription)
/// using the current ColorScheme.
/// </summary>
public interface ICellStylePalette
{
    /// <summary>
    /// Updates the palette with a new color scheme.
    /// Must be called whenever theme changes.
    /// </summary>
    void UpdateColorScheme(ColorScheme scheme);

    /// <summary>
    /// Resolves a visual style (font, colors, readonly) for the given logical state.
    /// </summary>
    CellStatusDescription Resolve(TableCellState state);
}