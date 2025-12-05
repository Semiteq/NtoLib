using System.Drawing;

namespace NtoLib.MbeTable.ModulePresentation.Rendering;

/// <summary>
/// Immutable DTO with final visual attributes used by the renderer.
/// </summary>
public readonly record struct CellRenderContext(
	Graphics Graphics,
	Rectangle Bounds,
	bool IsCurrent,
	Font Font,
	Color ForeColor,
	Color BackColor,
	object? FormattedValue);
