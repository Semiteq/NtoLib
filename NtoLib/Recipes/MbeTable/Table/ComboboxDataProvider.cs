using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.PinDataManager;
using NtoLib.Recipes.MbeTable.RecipeManager.Actions;

namespace NtoLib.Recipes.MbeTable.Table;

public class ComboboxDataProvider
{
    private readonly ActionManager _actionManager;
    private readonly IActionTargetProvider _actionTargetProvider;
    
    public ComboboxDataProvider(ActionManager actionManager, IActionTargetProvider actionTargetProvider)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
    }

    public IReadOnlyDictionary<int, string> GetActionTargets(int actionId)
    {
        var actionType = _actionManager.GetActionTypeById(actionId);
        return actionType switch
        {
            ActionType.Heater => _actionTargetProvider.GetHeaterNames(),
            ActionType.Shutter => _actionTargetProvider.GetShutterNames(),
            ActionType.NitrogenSource => _actionTargetProvider.GetNitrogenSourceNames(),
            _ => null
        };
    }
    
    public IReadOnlyDictionary<int, string> GetActions()
    {
        return _actionManager.GetAllActions();
    }
}