
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;

/// <summary>
/// Provides access to available action targets (e.g., heaters, shutters) by configuration-driven group names.
/// Backed by the current hardware configuration exposed via MbeTableFB pins.
/// </summary>
public interface IActionTargetProvider
{
    /// <summary>
    /// Refreshes the internal snapshot of targets from the MbeTableFB instance.
    /// Safe to call multiple times.
    /// </summary>
    void RefreshTargets();

    /// <summary>
    /// Tries to get the map of available targets for a given group.
    /// Keys are zero-based target IDs (0..N-1), values are display names (string).
    /// </summary>
    /// <param name="groupName">The configured hardware group name (e.g., "Shutters").</param>
    /// <param name="targets">The resulting read-only dictionary if found.</param>
    /// <returns>True if the group exists; otherwise, false.</returns>
    bool TryGetTargets(string groupName, out IReadOnlyDictionary<int, string> targets);

    /// <summary>
    /// Returns the minimal valid target ID for the specified group.
    /// </summary>
    /// <param name="groupName">The configured hardware group name.</param>
    /// <returns>The smallest available ID for that group.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the group does not exist or has no targets.</exception>
    int GetMinimalTargetId(string groupName);

    /// <summary>
    /// Returns a snapshot of all targets grouped by group name.
    /// Keys: group name; Values: read-only maps of target ID -> display name.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> GetAllTargetsSnapshot();
}