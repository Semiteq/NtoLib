using System;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;

public sealed class CellValueEventArgs : EventArgs
{
	public int RowIndex { get; }
	public int ColumnIndex { get; }
	public object? Value { get; set; }
	public CellValueEventArgs(int rowIndex, int columnIndex, object? value = null)
	{
		RowIndex = rowIndex;
		ColumnIndex = columnIndex;
		Value = value;
	}
}
