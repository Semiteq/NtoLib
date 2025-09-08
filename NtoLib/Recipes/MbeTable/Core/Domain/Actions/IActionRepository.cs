#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <summary>
/// Provides a centralized repository for accessing action definitions loaded from the application configuration.
/// </summary>
public interface IActionRepository
{
    /// <summary>
    /// Retrieves a specific action definition by its unique identifier.
    /// </summary>
    /// <param name="id">The ID of the action to retrieve.</param>
    /// <returns>The <see cref="ActionDefinition"/> if found; otherwise, throws a <see cref="KeyNotFoundException"/>.</returns>
    ActionDefinition GetActionById(int id);

    /// <summary>
    /// Gets a dictionary of all available actions, suitable for populating UI controls like comboboxes.
    /// </summary>
    /// <returns>A dictionary where the key is the action ID and the value is its display name.</returns>
    IReadOnlyDictionary<int, string> GetAllActions();
}