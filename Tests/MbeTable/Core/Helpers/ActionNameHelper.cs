using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace Tests.MbeTable.Core.Helpers;

/// <summary>
/// Resolves action ids by display name for readability in tests.
/// </summary>
public static class ActionNameHelper
{
	public static short GetActionIdOrThrow(ActionRepository repo, string name)
	{
		if (repo == null)
			throw new ArgumentNullException(nameof(repo));

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		var result = repo.GetResultActionDefinitionByName(name);
		if (result.IsFailed)
			throw new InvalidOperationException(
				$"Action '{name}' not found. Errors: {string.Join(", ", result.Errors)}");

		return result.Value.Id;
	}

	public static Result<short> GetActionId(ActionRepository repo, string name)
	{
		return string.IsNullOrWhiteSpace(name)
			? Result.Fail("Name empty")
			: repo.GetResultActionDefinitionByName(name).ToResult(r => r.Id);
	}
}
