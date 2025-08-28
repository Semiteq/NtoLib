#nullable enable

using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Composition;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <inheritdoc />
public sealed class ActionRepository : IActionRepository
{
    private readonly IReadOnlyDictionary<int, ActionDefinition> _actions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionRepository"/> class.
    /// </summary>
    /// <param name="appConfiguration">The loaded application configuration containing all action definitions.</param>
    public ActionRepository(AppConfiguration appConfiguration)
    {
        _actions = appConfiguration.Actions;
    }

    /// <inheritdoc />
    public ActionDefinition GetActionById(int id)
    {
        if (_actions.TryGetValue(id, out var action))
        {
            return action;
        }
        throw new KeyNotFoundException($"Action with ID {id} not found in the configuration.");
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, string> GetAllActions()
    {
        // Project the full definitions into a simpler dictionary for UI needs.
        return _actions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name);
    }
}