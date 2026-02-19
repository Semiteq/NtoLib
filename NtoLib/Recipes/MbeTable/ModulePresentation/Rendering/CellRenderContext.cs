using System.Drawing;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

public readonly record struct CellRenderContext(
	Graphics Graphics,
	Rectangle Bounds,
	bool IsCurrent,
	Font Font,
	Color ForeColor,
	Color BackColor,
	object? FormattedValue);
