using System;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;

public interface ITableView
{
	int RowCount { get; set; }
	void Invalidate();
	void InvalidateRow(int rowIndex);
	void EnsureRowVisible(int rowIndex);
	int CurrentRowIndex { get; }
	ColumnIdentifier? GetColumnKey(int columnIndex);
	event EventHandler<CellValueEventArgs> CellValueNeeded;
	event EventHandler<CellValueEventArgs> CellValuePushed;
}
