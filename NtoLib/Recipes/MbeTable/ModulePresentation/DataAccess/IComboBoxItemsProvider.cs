using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

public interface IComboBoxItemsProvider
{
	List<KeyValuePair<short, string>> GetItems(int rowIndex, ColumnIdentifier columnKey);
}
