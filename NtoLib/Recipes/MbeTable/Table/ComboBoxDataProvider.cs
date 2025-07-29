using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions;

namespace NtoLib.Recipes.MbeTable.Table
{
    public class ComboBoxDataProvider
    {
        public IReadOnlyDictionary<int, string> Actions { get; }
        private readonly ActionManager _actionManager;
        private readonly IFbActionTarget _fb;

        public ComboBoxDataProvider(ActionManager actionManager, IFbActionTarget fb)
        {
            _actionManager = actionManager ?? throw new System.ArgumentNullException(nameof(actionManager));
            
            // Actions are constant in runtime
            Actions = actionManager.GetAllActionsAsDictionary();
            
            // ActionTarget may vary in runtime
            _fb = fb ?? throw new System.ArgumentNullException(nameof(fb));
        }

        public bool TryGetTargetsForAction(int actionId, out Dictionary<int, string> targets, out string errorString)
        {
            targets = new Dictionary<int, string>();
            errorString = string.Empty;

            if (!_actionManager.GetActionEntryById(actionId, out var actionEntry, out errorString))
                return false;
            

            switch (actionEntry.ActionType)
            {
                case ActionType.Shutter:
                    targets = _fb.GetShutterNames();
                    return true;
                case ActionType.Heater:
                    targets = _fb.GetHeaterNames();
                    return true;
                case ActionType.NitrogenSource:
                    targets = _fb.GetNitrogenSourceNames();
                    return true;
                case ActionType.Service:
                default:
                    errorString = "Unsupported action type.";
                    return false;
            }
        }
    }
}