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

    public void RefreshTargets()
    {
        // No-op by design. Data is fetched on demand from MbeTableFB.
    }

    public bool TryGetTargets(string groupName, out IReadOnlyDictionary<int, string> targets)
    {
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        targets = default!;

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

        return targets.Keys.Min();
    }

    public IReadOnlyCollection<string> GetDefinedGroups() => _fb.GetDefinedGroupNames();

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