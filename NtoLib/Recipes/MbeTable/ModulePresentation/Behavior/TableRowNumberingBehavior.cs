using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableRowNumberingBehavior : ITableGridBehavior
{
	private readonly DataGridView _table;
	private bool _attached;

	public TableRowNumberingBehavior(DataGridView table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.RowPostPaint += OnRowPostPaint;
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
			_table.RowPostPaint -= OnRowPostPaint;
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

	private static void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
	{
		var grid = (DataGridView)sender!;
		var text = (e.RowIndex + 1).ToString();
		var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);

		var style = grid.RowHeadersDefaultCellStyle;
		var font = style?.Font ?? grid.Font;
		var foreColor = style?.ForeColor.IsEmpty == false ? style.ForeColor : grid.ForeColor;

		TextRenderer.DrawText(
			e.Graphics,
			text,
			font,
			headerBounds,
			foreColor,
			TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
	}
}
