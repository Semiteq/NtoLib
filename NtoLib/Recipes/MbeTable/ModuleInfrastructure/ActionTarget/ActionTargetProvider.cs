using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.Errors;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;

public sealed class ActionTargetProvider : IActionTargetProvider
{
    private readonly MbeTableFB _mbeTableFb;

    public ActionTargetProvider(MbeTableFB mbeTableFb)
    {
        _mbeTableFb = mbeTableFb ?? throw new ArgumentNullException(nameof(mbeTableFb));
    }

    public Result<int> GetMinimalTargetId(string? groupName)
    {
        var targetsResult = GetFilteredGroupTargets(groupName);
        if (targetsResult.IsFailed)
            return targetsResult.ToResult();

        var targets = targetsResult.Value;
        if (targets.Count == 0)
            return new InfrastructureTargetGroupEmptyError(groupName);

        var nonEmptyValueTargets = targets
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .ToDictionary(kv => (short)kv.Key, kv => kv.Value);

        if (nonEmptyValueTargets.Count == 0)
            return new InfrastructureTargetGroupNoNonEmptyError(groupName);

        return nonEmptyValueTargets.Min(kvp => kvp.Key);
    }

    public Result<IReadOnlyDictionary<short, string>> GetFilteredGroupTargets(string? groupName)
    {
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));

        var groupExists = _mbeTableFb.GetDefinedGroupNames()
            .Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase));

        if (!groupExists)
            return new InfrastructureTargetsNotDefinedError(groupName);

        var targets = _mbeTableFb.ReadTargets(groupName);
        var filteredTargets = targets.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .ToDictionary(kv => (short)kv.Key, kv => kv.Value);
        return Result.Ok(CreateReadOnlyDictionary(filteredTargets));
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> GetAllTargetsFilteredSnapshot()
    {
        var groups = _mbeTableFb.GetDefinedGroupNames();
        var result = new Dictionary<string, IReadOnlyDictionary<short, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var targets = _mbeTableFb.ReadTargets(group);
            var filteredTargets = targets.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .ToDictionary(kv => (short)kv.Key, kv => kv.Value);
            result[group] = CreateReadOnlyDictionary(filteredTargets);
        }

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<short, string>>(result);
    }

    private static IReadOnlyDictionary<short, string> CreateReadOnlyDictionary(IDictionary<short, string> source)
    {
        return new ReadOnlyDictionary<short, string>(source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }
}