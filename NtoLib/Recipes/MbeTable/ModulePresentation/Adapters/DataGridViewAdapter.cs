using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;

public sealed class DataGridViewAdapter : ITableView, IDisposable
{
	private readonly DataGridView _grid;
	public event EventHandler<CellValueEventArgs>? CellValueNeeded;
	public event EventHandler<CellValueEventArgs>? CellValuePushed;

	public DataGridViewAdapter(DataGridView grid)
	{
		_grid = grid ?? throw new ArgumentNullException(nameof(grid));
		_grid.CellValueNeeded += OnCellValueNeededInternal;
		_grid.CellValuePushed += OnCellValuePushedInternal;
	}

	public int RowCount
	{
		get => _grid.RowCount;
		set
		{
			if (_grid.IsDisposed)
			{
				return;
			}

			if (_grid.InvokeRequired)
			{
				try
				{
					_grid.BeginInvoke(new Action(() => _grid.RowCount = value));
				}
				catch
				{
					// ignored
				}
			}
			else
			{
				_grid.RowCount = value;
			}
		}
	}

	public void Invalidate()
	{
		if (_grid.IsDisposed)
		{
			return;
		}

		if (_grid.InvokeRequired)
		{
			try
			{
				_grid.BeginInvoke(new Action(() => _grid.Invalidate()));
			}
			catch
			{
				// ignored
			}
		}
		else
		{
			_grid.Invalidate();
		}
	}

	public void InvalidateRow(int rowIndex)
	{
		if (_grid.IsDisposed)
		{
			return;
		}

		if (rowIndex < 0)
		{
			return;
		}

		if (_grid.InvokeRequired)
		{
			try
			{
				_grid.BeginInvoke(new Action(() => InvalidateRow(rowIndex)));
			}
			catch
			{
				// ignored
			}

			return;
		}

		if (rowIndex < _grid.RowCount)
		{
			_grid.InvalidateRow(rowIndex);
		}
	}

	public void InvalidateCell(int columnIndex, int rowIndex)
	{
		if (_grid.IsDisposed)
		{
			return;
		}

		if (rowIndex < 0 || columnIndex < 0)
		{
			return;
		}

		if (_grid.InvokeRequired)
		{
			try
			{
				_grid.BeginInvoke(new Action(() => InvalidateCell(columnIndex, rowIndex)));
			}
			catch
			{
				// ignored
			}

			return;
		}

		if (rowIndex < _grid.RowCount && columnIndex < _grid.ColumnCount)
		{
			_grid.InvalidateCell(columnIndex, rowIndex);
		}
	}

	public void EnsureRowVisible(int rowIndex)
	{
		if (_grid.IsDisposed)
		{
			return;
		}

		if (rowIndex < 0)
		{
			return;
		}

		if (_grid.InvokeRequired)
		{
			try
			{
				_grid.BeginInvoke(new Action(() => EnsureRowVisible(rowIndex)));
			}
			catch
			{
				// ignored
			}

			return;
		}

		if (!_grid.IsHandleCreated)
		{
			return;
		}

		if (rowIndex >= _grid.RowCount)
		{
			return;
		}

		try
		{
			var first = _grid.FirstDisplayedScrollingRowIndex;
			var visible = _grid.DisplayedRowCount(false);

			if (first < 0 || visible <= 0 || rowIndex < first || rowIndex >= first + visible)
			{
				_grid.FirstDisplayedScrollingRowIndex = rowIndex;
			}
		}
		catch
		{
			// ignored
		}
	}

	public int CurrentRowIndex => _grid.CurrentCell?.RowIndex ?? -1;

	public ColumnIdentifier? GetColumnKey(int columnIndex)
	{
		if (columnIndex < 0 || columnIndex >= _grid.ColumnCount)
		{
			return null;
		}

		var column = _grid.Columns[columnIndex];
		var key = !string.IsNullOrEmpty(column.Name)
			? new ColumnIdentifier(column.Name)
			: null;

		return key;
	}

	private void OnCellValueNeededInternal(object? sender, DataGridViewCellValueEventArgs e)
	{
		var args = new CellValueEventArgs(e.RowIndex, e.ColumnIndex);
		CellValueNeeded?.Invoke(this, args);
		e.Value = args.Value;
	}

	private void OnCellValuePushedInternal(object? sender, DataGridViewCellValueEventArgs e)
	{
		var args = new CellValueEventArgs(e.RowIndex, e.ColumnIndex, e.Value);
		CellValuePushed?.Invoke(this, args);
	}


	public void Dispose()
	{
		try
		{
			_grid.CellValueNeeded -= OnCellValueNeededInternal;
			_grid.CellValuePushed -= OnCellValuePushedInternal;
		}
		catch
		{
			// ignored
		}
	}
}
