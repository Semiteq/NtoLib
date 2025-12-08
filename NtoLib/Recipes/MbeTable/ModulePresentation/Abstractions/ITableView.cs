using System;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Abstractions;

/// <summary>
/// UI-agnostic abstraction over a table-like control (DataGridView in current WinForms UI).
/// Allows unit-testing and future UI replacement without touching business logic.
/// </summary>
public interface ITableView
{
	/// <summary>
	/// Gets or sets current virtual row count.
	/// </summary>
	int RowCount { get; set; }

	/// <summary>
	/// True when native window handle is created.
	/// </summary>
	bool IsHandleCreated { get; }

	/// <summary>
	/// True when control is already disposed.
	/// </summary>
	bool IsDisposed { get; }

	/// <summary>
	/// Invalidates entire table forcing repaint.
	/// </summary>
	void Invalidate();

	/// <summary>
	/// Invalidates a single row.
	/// </summary>
	/// <param name="rowIndex">Zero-based row index.</param>
	void InvalidateRow(int rowIndex);

	/// <summary>
	/// Invalidates a single cell.
	/// </summary>
	/// <param name="columnIndex">Column index.</param>
	/// <param name="rowIndex">Row index.</param>
	void InvalidateCell(int columnIndex, int rowIndex);

	/// <summary>
	/// Ensures that the specified row is visible inside the viewport.
	/// </summary>
	/// <param name="rowIndex">Target row index.</param>
	void EnsureRowVisible(int rowIndex);

	/// <summary>
	/// Executes the action on UI thread if required.
	/// </summary>
	/// <param name="action">UI action.</param>
	void BeginInvoke(Action action);

	/// <summary>
	/// Current selected row index or –1 if nothing is selected.
	/// </summary>
	int CurrentRowIndex { get; }

	/// <summary>
	/// Gets the column key for the specified column index.
	/// </summary>
	/// <param name="columnIndex">Column index.</param>
	/// <returns>Column identifier or null if column not found.</returns>
	ColumnIdentifier? GetColumnKey(int columnIndex);

	/// <summary>
	/// Raised when the table requires a value for the given cell (VirtualMode).
	/// </summary>
	event EventHandler<CellValueEventArgs> CellValueNeeded;

	/// <summary>
	/// Raised when the user pushes a new value to a cell (VirtualMode).
	/// In VirtualMode, the CellValuePushed event is raised when the user changes a cell value and the new value must be saved to the external data source.
	/// </summary>
	event EventHandler<CellValueEventArgs> CellValuePushed;

	/// <summary>
	/// Raised when current cell (hence current row) has been changed.
	/// </summary>
	event EventHandler CurrentCellChanged;
}
