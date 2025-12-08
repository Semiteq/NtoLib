using System;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Abstractions;

/// <summary>
/// Event arguments used by <see cref="ITableView"/> for virtual data exchange.
/// Mirrors <see cref="System.Windows.Forms.DataGridViewCellValueEventArgs"/> without tight WinForms dependency.
/// </summary>
public sealed class CellValueEventArgs : EventArgs
{
	/// <summary>
	/// Zero-based row index.
	/// </summary>
	public int RowIndex { get; }

	/// <summary>
	/// Zero-based column index.
	/// </summary>
	public int ColumnIndex { get; }

	/// <summary>
	/// Cell value. For <c>CellValueNeeded</c> handlers set this property;
	/// for <c>CellValuePushed</c> handlers read the supplied value.
	/// </summary>
	public object? Value { get; set; }

	/// <summary>
	/// Initializes new instance of <see cref="CellValueEventArgs"/>.
	/// </summary>
	public CellValueEventArgs(int rowIndex, int columnIndex, object? value = null)
	{
		RowIndex = rowIndex;
		ColumnIndex = columnIndex;
		Value = value;
	}
}
