using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;

namespace NtoLib.Recipes.MbeTable.Recipe.ActionTargets;

public class ActionTargetExtractor
{
    //public bool TryGetTargetsForAction(int actionId, out IReadOnlyDictionary<int, string> targets, out string error)
    //{
    //var actionEntry = ActionManager.GetActionEntryById(actionId);
    //switch (actionEntry.ActionType)
    //{
    //    case ActionType.Shutter:
    //        if (_cachedShutterNames == null)
    //        {
    //            _cachedShutterNames = _fbTarget.GetShutterNames();
    //        }
    //        targets = _cachedShutterNames;
    //        return true;

    //    case ActionType.Heater:
    //        if (_cachedHeaterNames == null)
    //        {
    //            _cachedHeaterNames = _fbTarget.GetHeaterNames();
    //        }
    //        targets = _cachedHeaterNames;
    //        return true;

    //    case ActionType.NitrogenSource:
    //        if (_cachedNitrogenSourceNames == null)
    //        {
    //            _cachedNitrogenSourceNames = _fbTarget.GetNitrogenSourceNames();
    //        }
    //        targets = _cachedNitrogenSourceNames;
    //        return true;

    //    case ActionType.Service:
    //        // Возвращаем один и тот же пустой объект, не создавая новый.
    //        targets = _emptyServiceTargets;
    //        return true;

    //    default:
    //        throw new NotSupportedException($"Action type {actionEntry.ActionType} is not supported for dynamic targets.");
    //}
    //}
}