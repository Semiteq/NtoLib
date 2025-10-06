using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;

/// <summary>
/// Default implementation of <see cref="IActionTargetProvider"/> backed by <see cref="MbeTableFB"/>.
/// </summary>
public sealed class ActionTargetProvider : IActionTargetProvider
{
    private readonly MbeTableFB _fb;

    // Snapshot of current targets. Outer and inner dictionaries are read-only to callers.
    private IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> _targetsByGroup =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>(new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase));

    public ActionTargetProvider(MbeTableFB fb)
    {
        _fb = fb ?? throw new ArgumentNullException(nameof(fb));
    }

    public void RefreshTargets()
    {
        var groups = _fb.GetDefinedGroupNames();
        var updated = new Dictionary<string, IReadOnlyDictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var map = _fb.ReadTargets(group);

            // Defensive copy to a read-only dictionary
            var readonlyMap = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>(map));
            updated[group] = readonlyMap;
        }

        _targetsByGroup = new ReadOnlyDictionary<string, IReadOnlyDictionary<int, string>>(updated);
    }

    public bool TryGetTargets(string groupName, out IReadOnlyDictionary<int, string> targets)
    {
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        return _targetsByGroup.TryGetValue(groupName, out targets!);
    }

    public int GetMinimalTargetId(string groupName)
    {
        if (!TryGetTargets(groupName, out var targets))
            throw new InvalidOperationException($"Target group '{groupName}' is not defined.");

        if (targets.Count == 0)
            throw new InvalidOperationException($"Target group '{groupName}' has no targets configured.");

        return targets.Keys.Min();
    }

    public IReadOnlyCollection<string> GetDefinedGroups() => _targetsByGroup.Keys.ToArray();

    public IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> GetAllTargetsSnapshot() => _targetsByGroup;
}