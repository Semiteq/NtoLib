using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Config.Models.Actions;

public interface IComboboxDataProvider
{
    /// <summary>
    /// Retrieves a list of valid action targets based on the specified action ID.
    /// </summary>
    /// <param name="actionId">The unique identifier of the action whose targets are to be fetched.</param>
    /// <returns>
    /// A list of key-value pairs, where the key represents the target's unique identifier
    /// and the value represents the target's display name.
    /// Returns null if the action ID is invalid or no targets are available for the specified action.
    /// </returns>
    List<KeyValuePair<int, string>> GetActionTargets(int actionId);

    /// <summary>
    /// Retrieves a list of actions available for display in a combobox or similar UI element.
    /// </summary>
    /// <returns>
    /// A list of key-value pairs representing the actions. The key is the unique identifier of the action,
    /// and the value is the display name of the action.
    /// Returns an empty list if no actions are available.
    /// </returns>
    List<KeyValuePair<int, string>> GetActions();
}