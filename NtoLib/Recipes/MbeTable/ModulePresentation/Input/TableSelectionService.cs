using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class TableSelectionService
{
	private readonly DataGridView _table;

	public TableSelectionService(DataGridView table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	public IReadOnlyList<int> GetSelectedRowIndices()
	{
		if (_table.IsDisposed || _table.RowCount == 0)
		{
			return Array.Empty<int>();
		}

		return _table.SelectedRows
			.Cast<DataGridViewRow>()
			.Select(row => row.Index)
			.Where(idx => idx >= 0 && idx < _table.RowCount)
			.Distinct()
			.OrderBy(idx => idx)
			.ToArray();
	}

	public int GetInsertionIndexAfterSelection(int rowCount)
	{
		rowCount = Math.Max(0, rowCount);

		var selected = GetSelectedRowIndices();
		if (selected.Count > 0)
		{
			return Clamp(selected[^1] + 1, 0, rowCount);
		}

		var current = _table.CurrentCell?.RowIndex ?? -1;
		if (current >= 0)
		{
			return Clamp(current + 1, 0, rowCount);
		}

		return 0;
	}

	private static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}

		if (value > max)
		{
			return max;
		}

		return value;
	}
}
