using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

namespace NtoLib.Recipes.MbeTable.Table;

public class ComboBoxDataProvider
{
    private readonly ActionManager _actionManager;
    private readonly IFbActionTarget _fbTarget;

    public IReadOnlyDictionary<int, string> Actions { get; }

    /// <summary>
    /// Initializes a new instance of the ComboBoxDataProvider class and preloads static data sources.
    /// </summary>
    public ComboBoxDataProvider(ActionManager actionManager, IFbActionTarget fbTarget)
    {
        _actionManager = actionManager;
        _fbTarget = fbTarget;

        Actions = _actionManager.GetAllActionsAsDictionary();
    }

    /// <summary>
    /// Retrieves a static data source by its key.
    /// </summary>
    /// <param name="dataSourceKey">The key identifying the data source.</param>
    /// <returns>The corresponding data source or null if not found.</returns>
    public object GetDataSource(string dataSourceKey)
    {
        switch (dataSourceKey)
        {
            case "Actions":
                return Actions.ToList();
            default:
                return null;
        }
    }

    /// <summary>
    /// Retrieves a dynamic data source based on the context of a row.
    /// </summary>
    /// <param name="dataSourceKey">The key identifying the data source.</param>
    /// <param name="rowViewModel">The view model representing the row context.</param>
    /// <returns>The corresponding dynamic data source or null if not found.</returns>
    public object GetDynamicDataSource(string dataSourceKey, DynamicStepViewModel rowViewModel)
    {
        switch (dataSourceKey)
        {
            case "ActionTargets":
                dynamic viewModel = rowViewModel;
                int currentActionId;

                try
                {
                    currentActionId = (int)viewModel.Action;
                }
                catch
                {
                    return new Dictionary<int, string>();
                }

                TryGetTargetsForAction(currentActionId, out var targets, out _);
                return targets;

            default:
                return null;
        }
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

        if (!_actionManager.GetActionEntryById(actionId, out var actionEntry, out error))
        {
            return false;
        }

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