using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable;

/// <summary>
/// Abstraction over the FB-side reading of dynamic target-name pin groups, decoupling
/// <see cref="ModuleInfrastructure.ActionTarget.ActionTargetProvider" /> from the concrete FB type so
/// that both the full FB and the editor-only FB can supply target names.
/// </summary>
public interface IPinGroupReader
{
	IReadOnlyCollection<string> GetDefinedGroupNames();

	IReadOnlyDictionary<int, string> ReadTargets(string groupName);
}
