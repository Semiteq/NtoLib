using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Models;

namespace NtoLib.Recipes.MbeTable.Presentation.State;

/// <summary>
/// Resolves the visual state for a specific table cell by composing row execution state
/// and property state from data a layer.
/// </summary>
public interface ICellStateResolver
{
    /// <summary>
    /// Resolves the visual state for a cell.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="viewModel">Recipe view model.</param>
    /// <returns>The resolved visual state.</returns>
    CellVisualState Resolve(int rowIndex, int columnIndex, RecipeViewModel viewModel);
}