#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Models.ActionTargets;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Validates pin groups configuration loaded from PinGroups.json.
/// </summary>
public sealed class PinGroupsValidator
{
    /// <summary>
    /// Validates the provided groups and throws an InvalidOperationException on any error.
    /// </summary>
    public void ValidateOrThrow(IReadOnlyCollection<PinGroupData> groups)
    {
        if (groups == null || groups.Count == 0)
            throw new InvalidOperationException("PinGroups.json is empty or invalid.");

        foreach (var g in groups)
        {
            if (string.IsNullOrWhiteSpace(g.GroupName))
                throw new InvalidOperationException("PinGroups.json contains a group with an empty GroupName.");

            if (g.PinQuantity <= 0)
                throw new InvalidOperationException($"Group '{g.GroupName}' has invalid PinQuantity={g.PinQuantity}. Must be > 0.");

            if (g.FirstPinId <= 0)
                throw new InvalidOperationException($"Group '{g.GroupName}' has invalid FirstPinId={g.FirstPinId}. Must be > 0.");

            // PinGroupId can be any positive unique id for FB groups
            if (g.PinGroupId <= 0)
                throw new InvalidOperationException($"Group '{g.GroupName}' has invalid PinGroupId={g.PinGroupId}. Must be > 0.");
        }

        var dupName = groups.GroupBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(gr => gr.Count() > 1)?.Key;
        if (dupName != null)
            throw new InvalidOperationException($"Duplicate GroupName detected in PinGroups.json: '{dupName}'.");

        var dupGroupId = groups.GroupBy(g => g.PinGroupId)
            .FirstOrDefault(gr => gr.Count() > 1)?.Key;
        if (dupGroupId.HasValue)
            throw new InvalidOperationException($"Duplicate PinGroupId detected in PinGroups.json: '{dupGroupId.Value}'.");

        // Non-overlapping pin id ranges
        var ranges = groups
            .Select(g => new { g.GroupName, Start = g.FirstPinId, End = g.FirstPinId + g.PinQuantity - 1 })
            .OrderBy(r => r.Start)
            .ToArray();

        for (var i = 1; i < ranges.Length; i++)
        {
            var prev = ranges[i - 1];
            var curr = ranges[i];
            if (curr.Start <= prev.End)
            {
                throw new InvalidOperationException(
                    $"Pin id ranges overlap: '{prev.GroupName}' [{prev.Start}..{prev.End}] and '{curr.GroupName}' [{curr.Start}..{curr.End}].");
            }
        }
    }
}