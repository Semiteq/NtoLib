#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.ActionTargets;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Defines a contract for loading pin groups configuration from a JSON file (PinGroups.json).
/// </summary>
public interface IPinGroupsDataLoader
{
    /// <summary>
    /// Loads and deserializes pin groups configuration from the specified file path.
    /// </summary>
    /// <param name="configPath">Full file path to PinGroups.json.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="PinGroupData"/> on success,
    /// or an error with diagnostic information on failure.
    /// </returns>
    Result<List<PinGroupData>> LoadPinGroups(string configPath);
}