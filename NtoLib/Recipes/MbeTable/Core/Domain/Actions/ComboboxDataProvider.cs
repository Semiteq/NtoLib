#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

public class ComboboxDataProvider : IComboboxDataProvider
{
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _actionTargetProvider;
    
    public ComboboxDataProvider(IActionRepository actionRepository, IActionTargetProvider actionTargetProvider)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
    }

    /// <inheritdoc />
    public List<KeyValuePair<int, string>>? GetActionTargets(int actionId)
    {
        var actionType = _actionRepository.GetActionById(actionId).ActionType;
        return actionType switch
        {
            ActionType.Heater => _actionTargetProvider.GetHeaterNames().ToList(),
            ActionType.Shutter => _actionTargetProvider.GetShutterNames().ToList(),
            ActionType.NitrogenSource => _actionTargetProvider.GetNitrogenSourceNames().ToList(),
            _ => null
        };
    }
    
    /// <inheritdoc />   
    public List<KeyValuePair<int, string>> GetActions() => _actionRepository.GetAllActions().ToList();
}