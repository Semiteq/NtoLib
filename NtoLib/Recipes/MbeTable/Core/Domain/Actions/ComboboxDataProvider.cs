#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <summary>
/// Provides data for UI comboboxes (actions and their targets) based on configuration.
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

    /// <inheritdoc />
    public List<KeyValuePair<int, string>> GetActionTargets(int actionId)
    {
        var action = _actionRepository.GetActionById(actionId);
        var groupName = action.TargetGroup;

        if (string.IsNullOrWhiteSpace(groupName))
            return new List<KeyValuePair<int, string>>();

        return _actionTargetProvider.TryGetTargets(groupName, out var dict)
            ? dict.ToList()
            : new List<KeyValuePair<int, string>>();
    }

    /// <inheritdoc />
    public List<KeyValuePair<int, string>> GetActions() => _actionRepository.GetAllActions().ToList();
}