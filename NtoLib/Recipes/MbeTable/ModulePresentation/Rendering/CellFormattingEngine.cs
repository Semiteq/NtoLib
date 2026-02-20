using System.Collections.Generic;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Cells;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

/// <summary>
/// Resolves the visual state of each cell (colors, font, read-only flag,
/// combo display style) and applies it to cell styles and tags.
/// </summary>
internal sealed class CellFormattingEngine
{
	private readonly CellStateResolver _cellStateResolver;
	private readonly DesignTimeColorSchemeProvider _colorSchemeProvider;
	private readonly IReadOnlyList<ColumnDefinition> _columns;
	private readonly RecipeViewModel _recipeViewModel;
	private readonly ThreadSafeRowExecutionStateProvider _rowExecutionStateProvider;
	private readonly DataGridView _table;

	public CellFormattingEngine(
		DataGridView table,
		CellStateResolver cellStateResolver,
		RecipeViewModel recipeViewModel,
		ThreadSafeRowExecutionStateProvider rowExecutionStateProvider,
		DesignTimeColorSchemeProvider colorSchemeProvider,
		IReadOnlyList<ColumnDefinition> columns)
	{
		_table = table;
		_cellStateResolver = cellStateResolver;
		_recipeViewModel = recipeViewModel;
		_rowExecutionStateProvider = rowExecutionStateProvider;
		_colorSchemeProvider = colorSchemeProvider;
		_columns = columns;
	}

	public bool IsValidCellCoordinate(int rowIndex, int columnIndex)
	{
		return columnIndex < _columns.Count && rowIndex < _recipeViewModel.ViewModels.Count;
	}

	public CellVisualState ResolveCellVisualState(int rowIndex, int columnIndex)
	{
		var availability = _cellStateResolver.ResolveAvailability(rowIndex, columnIndex, _recipeViewModel);
		var scheme = _colorSchemeProvider.Current;
		var executionState = _rowExecutionStateProvider.GetState(rowIndex);
		var restricted = availability.IsReadOnly;

		var depth = _recipeViewModel.GetLoopNesting(rowIndex);
		var afterLoopBg = ColorStyleHelpers.ApplyLoopTint(availability.BackColor, depth, restricted, scheme);

		var afterExecutionBg = ColorStyleHelpers.ApplyExecutionTint(afterLoopBg, executionState, restricted, scheme);
		var finalFont = executionState switch
		{
			RowExecutionState.Current => scheme.SelectedLineFont,
			RowExecutionState.Passed => scheme.PassedLineFont,
			_ => availability.Font
		};

		var foreAfterContrast = ColorStyleHelpers.EnsureContrast(afterExecutionBg, availability.ForeColor);
		var final = new CellVisualState(
			Font: finalFont,
			ForeColor: foreAfterContrast,
			BackColor: afterExecutionBg,
			IsReadOnly: availability.IsReadOnly,
			ComboDisplayStyle: availability.ComboDisplayStyle);

		if (IsRowSelectedByUser(rowIndex) && executionState == RowExecutionState.Upcoming)
		{
			var selectedBg = ColorStyleHelpers.Blend(final.BackColor, scheme.RowSelectionBgColor, 0.35f);
			var adjustedFore = ColorStyleHelpers.EnsureContrast(selectedBg, final.ForeColor);
			final = final with { BackColor = selectedBg, ForeColor = adjustedFore };
		}

		return final;
	}

	public void ApplyVisualStateToCell(
		int rowIndex,
		int columnIndex,
		CellVisualState visual,
		DataGridViewCellStyle? targetStyle)
	{
		if (targetStyle != null)
		{
			ApplyVisualStateToStyle(visual, targetStyle);
		}

		var cell = _table.Rows[rowIndex].Cells[columnIndex];
		cell.Tag = visual;

		UpdateCellReadOnlyState(rowIndex, columnIndex, cell, visual.IsReadOnly);
		UpdateComboBoxDisplayStyle(cell, visual.ComboDisplayStyle);

		if (targetStyle == null)
		{
			ApplyVisualStateToCellStyle(cell, visual);
		}
	}

	private bool IsRowSelectedByUser(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= _table.Rows.Count)
		{
			return false;
		}

		return _table.Rows[rowIndex].Selected;
	}

	private static void ApplyVisualStateToStyle(CellVisualState visual, DataGridViewCellStyle style)
	{
		style.Font = visual.Font;
		style.ForeColor = visual.ForeColor;
		style.BackColor = visual.BackColor;
		style.SelectionBackColor = visual.BackColor;
		style.SelectionForeColor = visual.ForeColor;
	}

	private void UpdateCellReadOnlyState(int rowIndex, int columnIndex, DataGridViewCell cell, bool isReadOnly)
	{
		if (IsCellCurrentlyEditing(rowIndex, columnIndex))
		{
			return;
		}

		cell.ReadOnly = isReadOnly;
	}

	private bool IsCellCurrentlyEditing(int rowIndex, int columnIndex)
	{
		return _table.IsCurrentCellInEditMode
			   && _table.CurrentCell.RowIndex == rowIndex
			   && _table.CurrentCell.ColumnIndex == columnIndex;
	}

	private static void UpdateComboBoxDisplayStyle(DataGridViewCell cell, DataGridViewComboBoxDisplayStyle displayStyle)
	{
		switch (cell)
		{
			case RecipeComboBoxCell recipeCombo:
				recipeCombo.DisplayStyle = displayStyle;

				break;
			case DataGridViewComboBoxCell combo:
				combo.DisplayStyle = displayStyle;

				break;
		}
	}

	private static void ApplyVisualStateToCellStyle(DataGridViewCell cell, CellVisualState visual)
	{
		if (!cell.HasStyle)
		{
			return;
		}

		var currentStyle = cell.InheritedStyle;
		if (!ShouldUpdateCellStyle(currentStyle, visual))
		{
			return;
		}

		cell.Style.Font = visual.Font;
		cell.Style.ForeColor = visual.ForeColor;
		cell.Style.BackColor = visual.BackColor;
		cell.Style.SelectionBackColor = visual.BackColor;
		cell.Style.SelectionForeColor = visual.ForeColor;
	}

	private static bool ShouldUpdateCellStyle(DataGridViewCellStyle currentStyle, CellVisualState visual)
	{
		return !Equals(currentStyle.Font, visual.Font) ||
			   currentStyle.ForeColor != visual.ForeColor ||
			   currentStyle.BackColor != visual.BackColor;
	}
}
