using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.State;

/// <summary>
/// Resolves the visual state for a specific table cell by composing row execution state
/// and cell data state according to priority rules.
/// </summary>
public interface ICellStateResolver
{
    /// <summary>
    /// Resolves the visual state for a cell.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="viewModel">The step view model for the row.</param>
    /// <param name="columnKey">The column identifier.</param>
    /// <returns>The resolved visual state.</returns>
    CellVisualState Resolve(int rowIndex, StepViewModel viewModel, ColumnIdentifier columnKey);
}