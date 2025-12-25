using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableEditModeBehavior : ITableGridBehavior
{
	private readonly DataGridView _table;
	private bool _attached;

	public TableEditModeBehavior(DataGridView table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.CellMouseDown += OnCellMouseDown;
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
			_table.CellMouseDown -= OnCellMouseDown;
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

	private void OnCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
		{
			return;
		}

		if (e.RowIndex < 0)
		{
			return;
		}

		switch (e.ColumnIndex)
		{
			case -1:
				_table.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
				return;
			case >= 0:
				_table.EditMode = DataGridViewEditMode.EditOnEnter;
				break;
		}
	}
}
