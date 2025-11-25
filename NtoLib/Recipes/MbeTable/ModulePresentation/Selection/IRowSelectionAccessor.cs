using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Selection;

/// <summary>
/// Abstraction over row selection in the recipe table.
/// Commands use this interface instead of accessing UI control directly.
/// </summary>
public interface IRowSelectionAccessor
{
	/// <summary>
	/// Returns a sorted list of selected row indices.
	/// Returns an empty list when no rows are selected.
	/// </summary>
	IReadOnlyList<int> GetSelectedRowIndices();

	/// <summary>
	/// Returns index to be used as insertion target after current selection.
	/// Uses last selected row index + 1 if selection exists, otherwise
	/// uses current row index + 1, or 0 when table is empty.
	/// </summary>
	int GetInsertionIndexAfterSelection(int rowCount);
}
