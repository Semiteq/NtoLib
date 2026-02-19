using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Resolves visual cell state combining execution state and property state from data layer.
/// Execution state (Current/Passed) takes priority - cells become readonly during execution.
/// </summary>
public sealed class CellStateResolver
{
	private readonly DesignTimeColorSchemeProvider _colorSchemeProvider;

	public CellStateResolver(DesignTimeColorSchemeProvider colorSchemeProvider)
	{
		_colorSchemeProvider = colorSchemeProvider;
	}

	public CellVisualState ResolveAvailability(int rowIndex, int columnIndex, RecipeViewModel viewModel)
	{
		var propertyState = viewModel.GetCellState(rowIndex, columnIndex);
		var dataState = MapPropertyStateToDataState(propertyState);
		var scheme = _colorSchemeProvider.Current;

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
