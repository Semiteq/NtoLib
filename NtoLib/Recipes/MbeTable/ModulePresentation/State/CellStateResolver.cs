using System;
using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Resolves visual cell state combining execution state and property state from data layer.
/// Execution state (Current/Passed) takes priority - cells become readonly during execution.
/// </summary>
public sealed class CellStateResolver : ICellStateResolver
{
	// New: availability-only resolution separated from layering concerns.
	public CellVisualState ResolveAvailability(int rowIndex, int columnIndex, RecipeViewModel viewModel)
	{
		var propertyState = viewModel.GetCellState(rowIndex, columnIndex);
		var dataState = MapPropertyStateToDataState(propertyState);
		var scheme = _colorSchemeProvider.Current;

		// Availability base style (no execution / loop / selection yet)
		return dataState switch
		{
			CellDataState.Normal => new CellVisualState(
				Font: scheme.LineFont,
				ForeColor: scheme.LineTextColor,
				BackColor: scheme.LineBgColor,
				IsReadOnly: false,
				ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.DropDownButton),
			CellDataState.ReadOnly => new CellVisualState(
				Font: scheme.BlockedFont,
				ForeColor: scheme.BlockedTextColor,
				BackColor: scheme.BlockedBgColor,
				IsReadOnly: true,
				ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),
			CellDataState.Disabled => new CellVisualState(
				Font: scheme.BlockedFont,
				ForeColor: scheme.BlockedTextColor,
				BackColor: scheme.BlockedBgColor,
				IsReadOnly: true,
				ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing),
			_ => new CellVisualState(
				Font: scheme.BlockedFont,
				ForeColor: scheme.BlockedTextColor,
				BackColor: scheme.BlockedBgColor,
				IsReadOnly: true,
				ComboDisplayStyle: DataGridViewComboBoxDisplayStyle.Nothing)
		};
	}

	private static Font GetFontForExecution(RowExecutionState state, ColorScheme scheme, Font baseFont) => state switch
	{
		RowExecutionState.Current => scheme.SelectedLineFont,
		RowExecutionState.Passed => scheme.PassedLineFont,
		_ => baseFont
	};

	private readonly IColorSchemeProvider _colorSchemeProvider;

	private readonly IRowExecutionStateProvider _rowExecutionStateProvider;

	public CellStateResolver(
		IRowExecutionStateProvider rowExecutionStateProvider,
		IColorSchemeProvider colorSchemeProvider)
	{
		_rowExecutionStateProvider = rowExecutionStateProvider ??
									 throw new ArgumentNullException(nameof(rowExecutionStateProvider));
		_colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
	}

	public CellVisualState Resolve(int rowIndex, int columnIndex, RecipeViewModel viewModel)
	{
		// Legacy composite kept for backwards compatibility: execution + availability only
		var availability = ResolveAvailability(rowIndex, columnIndex, viewModel);
		var rowState = _rowExecutionStateProvider.GetState(rowIndex);
		// Apply execution tint only (no loop, no selection) for legacy callers
		var scheme = _colorSchemeProvider.Current;
		bool restricted = availability.IsReadOnly; // treat readonly/disabled same for tint scaling
		var tintedBg = ColorStyleHelpers.ApplyExecutionTint(availability.BackColor, rowState, restricted, scheme);
		var adjustedFore = ColorStyleHelpers.EnsureContrast(tintedBg, availability.ForeColor);
		return availability with
		{
			BackColor = tintedBg,
			ForeColor = adjustedFore,
			Font = GetFontForExecution(rowState, scheme, availability.Font)
		};
	}

	private static CellDataState MapPropertyStateToDataState(PropertyState propertyState)
	{
		return propertyState switch
		{
			PropertyState.Disabled => CellDataState.Disabled,
			PropertyState.Readonly => CellDataState.ReadOnly,
			PropertyState.Enabled => CellDataState.Normal,
			_ => CellDataState.Disabled
		};
	}
}
