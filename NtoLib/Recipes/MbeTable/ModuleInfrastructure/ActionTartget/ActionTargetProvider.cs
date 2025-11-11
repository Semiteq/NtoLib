using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;

public sealed class ActionTargetProvider : IActionTargetProvider
{
    private readonly MbeTableFB _fb;

    public ActionTargetProvider(MbeTableFB fb)
    {
        _fb = fb ?? throw new ArgumentNullException(nameof(fb));
    }

    public bool TryGetTargets(string groupName, out IReadOnlyDictionary<int, string>? targets)
    {
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        targets = null;
        
        var groups = _fb.GetDefinedGroupNames();
        var exists = groups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            return false;
        }

        var map = _fb.ReadTargets(groupName);
        targets = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>(map));
        return true;
    }

    public int GetMinimalTargetId(string groupName)
    {
        if (!TryGetTargets(groupName, out var targets))
            throw new InvalidOperationException($"Target group '{groupName}' is not defined.");

        if (targets.Count == 0)
            throw new InvalidOperationException($"Target group '{groupName}' has no targets configured.");

        var validTargets = targets.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToList();
        if (validTargets.Count == 0)
            throw new InvalidOperationException($"Target group '{groupName}' has no valid (non-empty) targets.");
        
        return validTargets.Min(kvp => kvp.Key);
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> GetAllTargetsSnapshot()
    {
        var groups = _fb.GetDefinedGroupNames();
        var result = new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var map = _fb.ReadTargets(group);
            result[group] = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>(map));
        }

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>(result);
    }
}