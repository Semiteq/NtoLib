using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableDataErrorBehavior : ITableGridBehavior
{
	private readonly DataGridView _table;
	private readonly StatusService? _statusService;
	private bool _attached;

	public TableDataErrorBehavior(DataGridView table, StatusService? statusService)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
		_statusService = statusService;
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.DataError += OnDataError;
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
			_table.DataError -= OnDataError;
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

	private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		var grid = (DataGridView)sender!;

		if (grid.Columns.Count <= e.ColumnIndex || grid.Rows.Count <= e.RowIndex)
		{
			e.ThrowException = false;
			e.Cancel = true;
			return;
		}

		var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

		if (cell is DataGridViewComboBoxCell || grid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
		{
			e.ThrowException = false;
			e.Cancel = false;
			return;
		}

		if (e.Exception is FormatException)
		{
			_statusService?.ShowError(e.Exception.Message);
			e.ThrowException = false;

			try
			{
				grid.CancelEdit();
				grid.EndEdit();
				grid.InvalidateCell(e.ColumnIndex, e.RowIndex);
			}
			catch
			{
				// ignored
			}
		}
		else
		{
			e.ThrowException = false;
			e.Cancel = true;
		}
	}
}
