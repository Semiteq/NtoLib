using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.Presentation.DataAccess;

/// <summary>
/// Supplies items for a DataGridViewComboBox editing control.
/// Does not decide cell state (Disabled/ReadOnly); only provides items.
/// </summary>
public interface IComboBoxItemsProvider
{
    /// <summary>
    /// Returns a list of id->display pairs for the given row/column.
    /// Never returns null; return an empty list when there are no items.
    /// </summary>
    List<KeyValuePair<int, string>> GetItems(int rowIndex, ColumnIdentifier columnKey);
}