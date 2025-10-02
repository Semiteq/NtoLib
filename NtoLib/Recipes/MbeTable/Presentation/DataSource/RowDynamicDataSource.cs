using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;

namespace NtoLib.Recipes.MbeTable.Presentation.DataSource;

/// <summary>
/// Strategy that queries the StepViewModel for row-specific dynamic items.
/// Returns items based on current Action (e.g., valve list for "Open" action).
/// Used for ActionTarget columns.
/// </summary>
public sealed class RowDynamicDataSource : IComboBoxDataSourceStrategy
{
    /// <inheritdoc />
    public List<KeyValuePair<int, string>>? GetItems(StepViewModel viewModel, ColumnIdentifier columnKey)
    {
        var dataState = viewModel.GetDataState(columnKey);

        if (dataState == CellDataState.Disabled)
        {
            return null;
        }

        var items = viewModel.GetComboItems(columnKey);

        if (items == null || items.Count == 0)
        {
            return new List<KeyValuePair<int, string>>();
        }

        return items;
    }
}