using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreActionNameNotFoundError : BilingualError
{
	public CoreActionNameNotFoundError(string actionName)
		: base(
			$"Action with name '{actionName}' not found",
			$"Действие с именем '{actionName}' не найдено")
	{
		ActionName = actionName;
	}

	public string ActionName { get; }
}
