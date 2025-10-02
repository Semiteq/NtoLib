using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.DataSource;

/// <summary>
/// Strategy interface for retrieving ComboBox datasource items.
/// Allows different columns to use static (column-level) or dynamic (row-level) data sources.
/// </summary>
public interface IComboBoxDataSourceStrategy
{
    /// <summary>
    /// Retrieves items for the ComboBox dropdown.
    /// </summary>
    /// <param name="viewModel">The StepViewModel for the current row.</param>
    /// <param name="columnKey">The column identifier.</param>
    /// <returns>
    /// Null if cell is disabled (no editor shown).
    /// Empty list to fallback to column-level static datasource.
    /// Populated list for dynamic row-specific items.
    /// </returns>
    List<KeyValuePair<int, string>>? GetItems(StepViewModel viewModel, ColumnIdentifier columnKey);
}