#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <summary>
/// Provides data for UI comboboxes (actions and per-column enum options).
/// </summary>
public sealed class ComboboxDataProvider : IComboboxDataProvider
{
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _actionTargetProvider;

    public ComboboxDataProvider(IActionRepository actionRepository, IActionTargetProvider actionTargetProvider)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
    }

    public List<KeyValuePair<int, string>> GetEnumOptions(int actionId, string columnKey)
    {
        var action = _actionRepository.GetActionById(actionId);
        var column = action.Columns.FirstOrDefault(c => string.Equals(c.Key, columnKey, StringComparison.OrdinalIgnoreCase));
        if (column is null) return new List<KeyValuePair<int, string>>();

        if (string.IsNullOrWhiteSpace(column.GroupName)) return new List<KeyValuePair<int, string>>();

        return _actionTargetProvider.TryGetTargets(column.GroupName!, out var dict)
            ? dict.ToList()
            : new List<KeyValuePair<int, string>>();
    }

    public List<KeyValuePair<int, string>> GetActions() => _actionRepository.GetAllActions().ToList();
}