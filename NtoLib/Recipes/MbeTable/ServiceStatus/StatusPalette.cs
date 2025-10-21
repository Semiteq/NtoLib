using System.Drawing;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

/// <summary>
/// Palette of colors for status rendering.
/// </summary>
public sealed record StatusPalette(
    Color InfoColor,
    Color WarningColor,
    Color ErrorColor,
    Color DefaultColor
);