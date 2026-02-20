using System;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

/// <summary>
/// Handles edit cancellation and dirty-state commit logic.
/// Decides whether a cell edit should proceed based on visual
/// state (read-only tag) and data state (disabled property).
/// </summary>
internal sealed class EditGatingCoordinator
{
	private readonly ILogger _logger;
	private readonly RecipeViewModel _recipeViewModel;
	private readonly DataGridView _table;

	public EditGatingCoordinator(
		DataGridView table,
		RecipeViewModel recipeViewModel,
		ILogger logger)
	{
		_table = table;
		_recipeViewModel = recipeViewModel;
		_logger = logger;
	}

	public void HandleCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
	{
		if (ShouldCancelEdit(e.RowIndex, e.ColumnIndex))
		{
			e.Cancel = true;
			_logger.LogDebug("Edit cancelled for cell [{Row},{Column}]", e.RowIndex, e.ColumnIndex);
		}
	}

	public void HandleCurrentCellDirtyStateChanged(object? sender, EventArgs e)
	{
		var cell = _table.CurrentCell;
		if (cell == null || !_table.IsCurrentCellDirty)
		{
			return;
		}

		if (IsCellDisabled(cell.RowIndex, cell.ColumnIndex))
		{
			_table.CancelEdit();

			return;
		}

		if (cell is not (DataGridViewComboBoxCell or RecipeComboBoxCell or DataGridViewCheckBoxCell))
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

	private bool ShouldCancelEdit(int rowIndex, int columnIndex)
	{
		if (rowIndex < 0 || columnIndex < 0)
		{
			return false;
		}

		try
		{
			if (IsCellReadOnlyByVisualState(rowIndex, columnIndex))
			{
				return true;
			}

			return IsCellDisabled(rowIndex, columnIndex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to validate edit permissions for cell [{Row},{Column}]", rowIndex,
				columnIndex);

			return true;
		}
	}

	private bool IsCellReadOnlyByVisualState(int rowIndex, int columnIndex)
	{
		var cell = _table.Rows[rowIndex].Cells[columnIndex];

		return cell.Tag is CellVisualState visual && visual.IsReadOnly;
	}

	private bool IsCellDisabled(int rowIndex, int columnIndex)
	{
		var state = _recipeViewModel.GetCellState(rowIndex, columnIndex);

		return state == PropertyState.Disabled;
	}
}
