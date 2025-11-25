using System;

using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;

/// <summary>
/// Provides the execution state (Passed/Current/Upcoming) for table rows based on PLC status.
/// </summary>
public interface IRowExecutionStateProvider
{
	/// <summary>
	/// Occurs when the current line changes, providing the old and new indices.
	/// </summary>
	event Action<int, int> CurrentLineChanged;

	/// <summary>
	/// Gets the execution state for the specified row.
	/// </summary>
	/// <param name="rowIndex">Zero-based row index.</param>
	/// <returns>The row execution state.</returns>
	RowExecutionState GetState(int rowIndex);

	/// <summary>
	/// Gets the current executing line index. Returns -1 if no recipe is active.
	/// </summary>
	int CurrentLineIndex { get; }
}
