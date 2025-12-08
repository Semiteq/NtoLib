using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Selection;

/// <summary>
/// Provides read-only access to the current row selection of a DataGridView.
/// Used by presentation commands to retrieve selected indices without
/// directly depending on DataGridView in higher layers.
/// </summary>
public sealed class DataGridViewRowSelectionAccessor : IRowSelectionAccessor
{
	private readonly DataGridView _grid;

	public DataGridViewRowSelectionAccessor(DataGridView grid)
	{
		_grid = grid ?? throw new ArgumentNullException(nameof(grid));
	}

	public IReadOnlyList<int> GetSelectedRowIndices()
	{
		if (_grid.IsDisposed || _grid.RowCount == 0)
			return Array.Empty<int>();

		// SelectedRows may not be sorted by index; normalize to ascending order.
		var indices = _grid.SelectedRows
			.Cast<DataGridViewRow>()
			.Select(row => row.Index)
			.Where(idx => idx >= 0 && idx < _grid.RowCount)
			.Distinct()
			.OrderBy(idx => idx)
			.ToList();

		return indices;
	}

	public int GetInsertionIndexAfterSelection(int rowCount)
	{
		if (rowCount < 0)
			throw new ArgumentOutOfRangeException(nameof(rowCount));

		var selected = GetSelectedRowIndices();
		if (selected.Count > 0)
		{
			var index = selected[selected.Count - 1] + 1;
			if (index < 0)
				index = 0;
			if (index > rowCount)
				index = rowCount;
			return index;
		}

		var current = _grid.CurrentCell?.RowIndex ?? -1;
		if (current >= 0)
		{
			var index = current + 1;
			if (index < 0)
				index = 0;
			if (index > rowCount)
				index = rowCount;
			return index;
		}

		return 0;
	}
}
