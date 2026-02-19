using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

public sealed class TargetItemsProvider : IComboBoxItemsProvider
{
	private readonly CellDataContext _context;

	public TargetItemsProvider(CellDataContext context)
	{
		_context = context;
	}

	public List<KeyValuePair<short, string>> GetItems(int rowIndex, ColumnIdentifier columnKey)
	{
		var vm = _context.GetStepViewModel(rowIndex);
		if (vm == null)
		{
			return new List<KeyValuePair<short, string>>();
		}

		var result = vm.GetComboItems(columnKey);
		if (result.IsFailed || result.Value == null)
		{
			return new List<KeyValuePair<short, string>>();
		}

		return result.Value
			.Select(kv => new KeyValuePair<short, string>(kv.Key, kv.Value))
			.ToList();
	}
}
