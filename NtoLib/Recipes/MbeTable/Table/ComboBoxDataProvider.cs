using System;
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.ActionTargets;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.Table;

public class ComboBoxDataProvider
{
    private readonly ActionManager _actionManager;
    private readonly IFbActionTarget _fbTarget;

    private readonly List<(int Id, string Name)> _actions;

    private Dictionary<int, string> _cachedShutterNames;
    private Dictionary<int, string> _cachedHeaterNames;
    private Dictionary<int, string> _cachedNitrogenSourceNames;
    private readonly Dictionary<int, string> _emptyServiceTargets = new Dictionary<int, string>();

    public ComboBoxDataProvider(ActionManager actionManager)
    {
        _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager), @"ActionManager cannot be null.");

        _actions = _actionManager.GetAllActionsAsList();
    }

    // public IReadOnlyList<(int Id, string Name)> GetActionTargets(StepViewModel stepViewModel)
    // { 
    //     if (stepViewModel == null)
    //     {
    //         throw new ArgumentNullException(nameof(stepViewModel), "StepViewModel cannot be null.");
    //     }
    //
    //     return IReadOnlyList<>
    // }
    
    
    public List<IReadOnlyDictionary<int, string>> GetActionDataSource(string dataSourceKey)
    {
        switch (dataSourceKey)
        {
            case "Actions":
                // return _actions;
            default:
                return null;
        }
    }
    
    public Dictionary<int, string> GetActionTargetDatasource(StepViewModel rowViewModel)
    {
        dynamic viewModel = rowViewModel;
        int currentActionId;

        
        
        currentActionId = (int)viewModel.Action;
        

        if (TryGetTargetsForAction(currentActionId, out var targets, out var error))
        {
            return targets;
        }

        throw new InvalidOperationException($"Failed to get action targets for action ID {currentActionId}: {error}");
    }

    /// <summary>
    /// Attempts to retrieve targets for a specific action by its ID.
    /// </summary>
    /// <param name="actionId">The ID of the action.</param>
    /// <param name="targets">The dictionary of targets retrieved.</param>
    /// <param name="error">An error message if the operation fails.</param>
    /// <returns>True if targets are successfully retrieved; otherwise, false.</returns>
    public bool TryGetTargetsForAction(int actionId, out Dictionary<int, string> targets, out string error)
    {
        targets = new Dictionary<int, string>();
        error = null;

        var actionEntry = _actionManager.GetActionEntryById(actionId);

        if (actionEntry.ActionType == ActionType.Shutter)
        {
            targets = _fbTarget.GetShutterNames();
            return true;
        }
        if (actionEntry.ActionType == ActionType.Heater)
        {
            targets = _fbTarget.GetHeaterNames();
            return true;
        }
        if (actionEntry.ActionType == ActionType.NitrogenSource)
        {
            targets = _fbTarget.GetNitrogenSourceNames();
            return true;
        }
        if (actionEntry.ActionType == ActionType.Service)
        {
            targets = new Dictionary<int, string>();
            return true;
        }

        throw new NotSupportedException($"Action type {actionEntry.ActionType} is not supported for dynamic targets.");
    }
}