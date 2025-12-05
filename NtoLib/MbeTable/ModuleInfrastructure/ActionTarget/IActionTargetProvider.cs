using System.Collections.Generic;

using FluentResults;

namespace NtoLib.MbeTable.ModuleInfrastructure.ActionTarget;

public interface IActionTargetProvider
{
	Result<IReadOnlyDictionary<short, string>> GetFilteredGroupTargets(string groupName);

	Result<int> GetMinimalTargetId(string groupName);

	IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> GetAllTargetsFilteredSnapshot();
}
