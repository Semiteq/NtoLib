using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

public sealed class ActionItemsProvider : IComboBoxItemsProvider
{
	private readonly ComboboxDataProvider _provider;

	public ActionItemsProvider(ComboboxDataProvider provider)
	{
		_provider = provider;
	}

	public List<KeyValuePair<short, string>> GetItems(int rowIndex, ColumnIdentifier columnKey)
	{
		return _provider.GetActions()
			.Select(kv => new KeyValuePair<short, string>(kv.Key, kv.Value))
			.ToList();
	}
}
