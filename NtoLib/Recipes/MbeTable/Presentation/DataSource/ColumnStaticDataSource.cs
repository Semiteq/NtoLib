using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;

namespace NtoLib.Recipes.MbeTable.Presentation.DataSource;

/// <summary>
/// Strategy that uses the column-level static datasource.
/// Returns empty list to signal fallback to DataGridViewComboBoxColumn.DataSource.
/// Used for Action column.
/// </summary>
public sealed class ColumnStaticDataSource : IComboBoxDataSourceStrategy
{
    /// <inheritdoc />
    public List<KeyValuePair<int, string>>? GetItems(StepViewModel viewModel, ColumnIdentifier columnKey)
    {
        return new List<KeyValuePair<int, string>>();
    }
}