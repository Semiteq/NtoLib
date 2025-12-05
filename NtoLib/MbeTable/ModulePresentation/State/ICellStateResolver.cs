using NtoLib.MbeTable.ModuleApplication.ViewModels;
using NtoLib.MbeTable.ModulePresentation.Models;

namespace NtoLib.MbeTable.ModulePresentation.State;

/// <summary>
/// Resolves the visual state for a specific table cell by composing row execution state
/// and property state from data a layer.
/// </summary>
public interface ICellStateResolver
{
	/// <summary>
	/// Legacy full resolution (execution + availability) retained for backwards compatibility.
	/// </summary>
	CellVisualState Resolve(int rowIndex, int columnIndex, RecipeViewModel viewModel);

	/// <summary>
	/// Resolves only the availability base style (Enabled/ReadOnly/Disabled).
	/// </summary>
	CellVisualState ResolveAvailability(int rowIndex, int columnIndex, RecipeViewModel viewModel);
}
