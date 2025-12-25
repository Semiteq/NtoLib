using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableCellValidationBehavior : ITableGridBehavior
{
	private readonly DataGridView _table;
	private bool _attached;

	public TableCellValidationBehavior(DataGridView table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.CellValidating += OnCellValidating;
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
			_table.CellValidating -= OnCellValidating;
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

	private void OnCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		if (!_table.IsCurrentCellInEditMode)
		{
			return;
		}

		if (!_table.IsCurrentCellDirty)
		{
			return;
		}

		try
		{
			_table.CommitEdit(DataGridViewDataErrorContexts.Commit);
			_table.EndEdit();
		}
		catch
		{
			// ignored
		}
	}
}
