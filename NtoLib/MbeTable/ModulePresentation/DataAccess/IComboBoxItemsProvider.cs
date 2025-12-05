using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.MbeTable.ModulePresentation.DataAccess;

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
	List<KeyValuePair<short, string>> GetItems(int rowIndex, ColumnIdentifier columnKey);
}
