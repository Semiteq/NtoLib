using System;
using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableCellPaintingBehavior : ITableGridBehavior
{
	private readonly ColorScheme _colorScheme;
	private readonly DataGridView _table;
	private bool _attached;

	public TableCellPaintingBehavior(DataGridView table, ColorScheme colorScheme)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.CellPainting += OnCellPainting;
		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
		{
			return;
		}

		try
		{
			_table.CellPainting -= OnCellPainting;
		}
		catch
		{
			// ignored
		}

		_attached = false;
	}

	public void Dispose()
	{
		Detach();
	}

	private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		var cell = _table.Rows[e.RowIndex].Cells[e.ColumnIndex];
		if (cell is RecipeComboBoxCell)
		{
			return;
		}

		e.Paint(e.ClipBounds, e.PaintParts);
		DrawFocusOutlineIfCurrent(_table, e);
		e.Handled = true;
	}

	private void DrawFocusOutlineIfCurrent(DataGridView grid, DataGridViewCellPaintingEventArgs e)
	{
		var currentCell = grid.CurrentCell;
		if (currentCell == null || currentCell.RowIndex != e.RowIndex || currentCell.ColumnIndex != e.ColumnIndex)
		{
			return;
		}

		using var pen = new Pen(_colorScheme.SelectedOutlineColor, Math.Max(1, _colorScheme.SelectedOutlineThickness));
		var rect = Rectangle.Inflate(e.CellBounds, -1, -1);
		e.Graphics.DrawRectangle(pen, rect);
	}
}
