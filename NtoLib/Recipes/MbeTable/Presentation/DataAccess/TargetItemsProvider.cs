using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.Presentation.DataAccess;

/// <summary>
/// Provides row-dependent target items (valves, sensors, etc.) for target ComboBox columns.
/// </summary>
public sealed class TargetItemsProvider : IComboBoxItemsProvider
{
    private readonly ICellDataContext _context;

    public TargetItemsProvider(ICellDataContext context)
    {
        _context = context;
    }

    public List<KeyValuePair<int, string>> GetItems(int rowIndex, ColumnIdentifier columnKey)
    {
        var vm = _context.GetStepViewModel(rowIndex);
        if (vm == null)
            return new List<KeyValuePair<int, string>>();

        var result = vm.GetComboItems(columnKey);
        if (result.IsFailed || result.Value == null)
            return new List<KeyValuePair<int, string>>();

        return result.Value
            .Select(kv => new KeyValuePair<int, string>(kv.Key, kv.Value))
            .ToList();
    }
}