using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Single owner of defaulted-cell mark state. An action change marks every editable
/// (non-Action, Enabled) cell of the affected row; marks clear per-cell on a value commit
/// and in bulk on recipe Send/Save/Load. Marks are transient UI state, never serialized.
/// </summary>
public sealed class DefaultedCellTracker : IDefaultedCellsReader, IDisposable
{
	private const int RowDropped = -1;

	private readonly IReadOnlyList<ColumnDefinition> _columns;
	private readonly Dictionary<int, HashSet<int>> _marks = new();
	private readonly object _marksLock = new();
	private readonly PropertyStateProvider _propertyStateProvider;
	private readonly RecipeFacade _recipeFacade;
	private readonly RecipeOperationService _operationService;

	private bool _disposed;

	public DefaultedCellTracker(
		RecipeOperationService operationService,
		RecipeFacade recipeFacade,
		PropertyStateProvider propertyStateProvider,
		IReadOnlyList<ColumnDefinition> columns)
	{
		_operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
		_recipeFacade = recipeFacade ?? throw new ArgumentNullException(nameof(recipeFacade));
		_propertyStateProvider = propertyStateProvider ?? throw new ArgumentNullException(nameof(propertyStateProvider));
		_columns = columns ?? throw new ArgumentNullException(nameof(columns));

		_operationService.ActionReplaced += OnActionReplaced;
		_operationService.CellValueCommitted += OnCellValueCommitted;
		_operationService.RecipeStructureChanged += OnRecipeStructureChanged;
		_operationService.RecipeSent += OnBulkClear;
		_operationService.RecipeSaved += OnBulkClear;
	}

	public event Action<MarksChange>? MarksChanged;

	public bool IsMarked(int row, int col)
	{
		lock (_marksLock)
		{
			return _marks.TryGetValue(row, out var columns) && columns.Contains(col);
		}
	}

	/// <summary>
	/// Clears a single mark addressed by grid column index. Used by the visited-and-left
	/// flow, which observes the grid and therefore works in column indices rather than keys.
	/// </summary>
	public void ClearCell(int row, int columnIndex)
	{
		bool changed;
		lock (_marksLock)
		{
			changed = RemoveMark(row, columnIndex);
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(row));
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_operationService.ActionReplaced -= OnActionReplaced;
		_operationService.CellValueCommitted -= OnCellValueCommitted;
		_operationService.RecipeStructureChanged -= OnRecipeStructureChanged;
		_operationService.RecipeSent -= OnBulkClear;
		_operationService.RecipeSaved -= OnBulkClear;
		_disposed = true;
	}

	private void OnActionReplaced(int row)
	{
		MarkRow(row);
	}

	private void OnCellValueCommitted((int Row, ColumnIdentifier Column) commit)
	{
		ClearCell(commit.Row, commit.Column);
	}

	private void OnRecipeStructureChanged(StructureChange change)
	{
		ApplyStructureChange(change);
	}

	private void OnBulkClear()
	{
		ClearAll();
	}

	private void MarkRow(int row)
	{
		bool changed;
		lock (_marksLock)
		{
			var steps = _recipeFacade.CurrentSnapshot.Recipe.Steps;
			if (row < 0 || row >= steps.Count)
			{
				return;
			}

			var markedColumns = ResolveMarkableColumns(steps[row]);
			if (markedColumns.Count == 0)
			{
				changed = _marks.Remove(row);
			}
			else
			{
				_marks[row] = markedColumns;
				changed = true;
			}
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(row));
		}
	}

	private HashSet<int> ResolveMarkableColumns(Step step)
	{
		var markedColumns = new HashSet<int>();
		for (var columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
		{
			var column = _columns[columnIndex];
			if (column.Key == MandatoryColumns.Action)
			{
				continue;
			}

			if (_propertyStateProvider.GetPropertyState(step, column.Key) == PropertyState.Enabled)
			{
				markedColumns.Add(columnIndex);
			}
		}

		return markedColumns;
	}

	private void ClearCell(int row, ColumnIdentifier column)
	{
		var columnIndex = IndexOf(column);
		if (columnIndex < 0)
		{
			return;
		}

		bool changed;
		lock (_marksLock)
		{
			changed = RemoveMark(row, columnIndex);
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(row));
		}
	}

	private bool RemoveMark(int row, int columnIndex)
	{
		if (!_marks.TryGetValue(row, out var columns) || !columns.Remove(columnIndex))
		{
			return false;
		}

		if (columns.Count == 0)
		{
			_marks.Remove(row);
		}

		return true;
	}

	private void ClearAll()
	{
		bool changed;
		lock (_marksLock)
		{
			changed = _marks.Count > 0;
			_marks.Clear();
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(null));
		}
	}

	private void ApplyStructureChange(StructureChange change)
	{
		switch (change.Kind)
		{
			case StructureChangeKind.Insert:
				ApplyInsert(change.Index, change.Count);
				break;
			case StructureChangeKind.Remove:
				ApplyRemove(change.RemovedIndices ?? Array.Empty<int>());
				break;
			case StructureChangeKind.Reset:
				ClearAll();
				break;
		}
	}

	private void ApplyInsert(int index, int count)
	{
		bool changed;
		lock (_marksLock)
		{
			if (_marks.Count == 0 || count <= 0)
			{
				return;
			}

			changed = RebuildMarks(row => row >= index ? row + count : row);
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(null));
		}
	}

	private void ApplyRemove(IReadOnlyList<int> removedIndices)
	{
		bool changed;
		lock (_marksLock)
		{
			if (_marks.Count == 0 || removedIndices.Count == 0)
			{
				return;
			}

			var removed = new HashSet<int>(removedIndices);
			changed = RebuildMarks(row =>
				removed.Contains(row) ? RowDropped : row - CountBelow(removedIndices, row));
		}

		if (changed)
		{
			RaiseMarksChanged(new MarksChange(null));
		}
	}

	private static int CountBelow(IReadOnlyList<int> indices, int row)
	{
		var count = 0;
		for (var i = 0; i < indices.Count; i++)
		{
			if (indices[i] < row)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Re-keys every mark through <paramref name="mapRow"/>. A return of <see cref="RowDropped"/>
	/// removes the mark entirely. Returns whether any row index actually changed.
	/// </summary>
	private bool RebuildMarks(Func<int, int> mapRow)
	{
		var shifted = new Dictionary<int, HashSet<int>>();
		var anyIndexShifted = false;
		foreach (var entry in _marks)
		{
			var newRow = mapRow(entry.Key);
			if (newRow == RowDropped)
			{
				anyIndexShifted = true;
				continue;
			}

			anyIndexShifted |= newRow != entry.Key;
			shifted[newRow] = entry.Value;
		}

		_marks.Clear();
		foreach (var entry in shifted)
		{
			_marks[entry.Key] = entry.Value;
		}

		return anyIndexShifted;
	}

	private int IndexOf(ColumnIdentifier column)
	{
		for (var index = 0; index < _columns.Count; index++)
		{
			if (_columns[index].Key == column)
			{
				return index;
			}
		}

		return -1;
	}

	private void RaiseMarksChanged(MarksChange change)
	{
		MarksChanged?.Invoke(change);
	}
}
