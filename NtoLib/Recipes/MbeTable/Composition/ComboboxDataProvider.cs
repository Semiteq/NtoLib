#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Composition;

public class ComboboxDataProvider : IComboboxDataProvider
{
    private readonly ActionManager _actionManager;
    private readonly IActionTargetProvider _actionTargetProvider;

    public ComboboxDataProvider(ActionManager actionManager, IActionTargetProvider actionTargetProvider)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
    }

    public List<KeyValuePair<int, string>>? GetActionTargets(int actionId)
    {
        var actionType = _actionManager.GetActionTypeById(actionId);
        return actionType switch
        {
            ActionType.Heater => _actionTargetProvider.GetHeaterNames().ToList(),
            ActionType.Shutter => _actionTargetProvider.GetShutterNames().ToList(),
            ActionType.NitrogenSource => _actionTargetProvider.GetNitrogenSourceNames().ToList(),
            _ => null
        };
    }

    public List<KeyValuePair<int, string>> GetActions() => _actionManager.GetAllActions().ToList();
}