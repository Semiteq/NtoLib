using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

public interface IActionsLoader
{
    /// <summary>
    /// Loads action definitions from a configuration file, deserializes them, and validates against duplication of IDs.
    /// </summary>
    /// <param name="configPath">The path to the configuration file containing action definitions in JSON format.</param>
    /// <returns>A result containing a dictionary where the key is the action ID and the value is the action definition, or an error if loading or validation fails.</returns>
    Result<Dictionary<int, ActionDefinition>> LoadActions(string configPath);
}