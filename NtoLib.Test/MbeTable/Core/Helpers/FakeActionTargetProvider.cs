using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;

namespace NtoLib.Test.MbeTable.Core.Helpers;

public sealed class FakeActionTargetProvider : IActionTargetProvider
{
    public Result<int> GetMinimalTargetId(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return Result.Fail("Group name is empty");

        return Result.Ok(1);
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> GetAllTargetsFilteredSnapshot()
    {
        return new Dictionary<string, IReadOnlyDictionary<short, string>>();
    }

    public Result<IReadOnlyDictionary<short, string>> GetFilteredGroupTargets(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return Result.Fail("Group name is empty");

        var dict = new Dictionary<short, string>
        {
            { 1, $"{groupName}_1" },
            { 2, $"{groupName}_2" }
        };
        return Result.Ok((IReadOnlyDictionary<short, string>)dict);
    }
}