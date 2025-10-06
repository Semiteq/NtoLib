using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Services;

namespace NtoLib.Recipes.MbeTable.Presentation.DataAccess;

/// <summary>
/// Provides static Action items for the Action ComboBox column.
/// </summary>
public sealed class ActionItemsProvider : IComboBoxItemsProvider
{
    private readonly IComboboxDataProvider _provider;

    public ActionItemsProvider(IComboboxDataProvider provider)
    {
        _provider = provider;
    }

    public List<KeyValuePair<int, string>> GetItems(int rowIndex, ColumnIdentifier columnKey)
    {
        return _provider.GetActions()
            .Select(kv => new KeyValuePair<int, string>(kv.Key, kv.Value))
            .ToList();
    }
}